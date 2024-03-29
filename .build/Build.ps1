param (
    [Parameter(Mandatory = $true)]
    [string]$CertificatePath,

    [Parameter(Mandatory=$true)]
    [string]$CertificatePassword,

    [Parameter(Mandatory=$true)]
    [string]$SignToolPath,

    [Parameter(Mandatory=$true)]
    [string]$KeystorePassword,

    [switch]$BuildAgent,

    [switch]$BuildViewer,

    [switch]$BuildStreamer,

    [switch]$IncrementAndroidVersion
)

$InstallerDir = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer"
$VsWhere = "$InstallerDir\vswhere.exe"
$MSBuildPath = (&"$VsWhere" -latest -products * -find "\MSBuild\Current\Bin\MSBuild.exe").Trim()
$Root = (Get-Item -Path $PSScriptRoot).Parent.FullName
$DownloadsFolder = "$Root\ControlR.Server\wwwroot\downloads"
$Now = [System.DateTime]::UtcNow
$CurrentVersion = $Now.ToString("yyyy.MM.dd.HHmm")

if (!(Test-Path $CertificatePath)) {
    Write-Error "Certificate not found."
    return
}

if (!(Test-Path $SignToolPath)) {
    Write-Error "SignTool not found."
    return
}

Set-Location $Root

if (!(Test-Path -Path "$Root\ControlR.sln")) {
    Write-Host "Unable to determine solution directory." -ForegroundColor Red
    return
}

if ($BuildAgent){
    dotnet publish --configuration Release -p:PublishProfile=win-x86 -p:Version=$CurrentVersion -p:FileVersion=$CurrentVersion -p:IncludeAllContentForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:IncludeAppSettingsInSingleFile=true  "$Root\ControlR.Agent\"
    dotnet publish --configuration Release -p:PublishProfile=ubuntu-x64 -p:Version=$CurrentVersion -p:FileVersion=$CurrentVersion -p:IncludeAllContentForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:IncludeAppSettingsInSingleFile=true  "$Root\ControlR.Agent\"
    Start-Sleep -Seconds 1
    &"$SignToolPath" sign /fd SHA256 /f "$CertificatePath" /p $CertificatePassword /t http://timestamp.digicert.com "$DownloadsFolder\ControlR.Agent.exe"
}

if ($BuildStreamer) {
    [string]$PackageJson = Get-Content -Path "$Root\ControlR.Streamer\package.json"
    $Package = $PackageJson | ConvertFrom-Json
    $Package.version = $Now.ToString("yyyy.MM.ddHHmm")
    [string]$PackageJson = $Package | ConvertTo-Json
    [System.IO.File]::WriteAllText("$Root\ControlR.Streamer\package.json", $PackageJson)
    Push-Location "$Root\ControlR.Streamer"
    npm install
    npm run make-pwsh
    Pop-Location
}

if ($BuildViewer) {
    if ($IncrementAndroidVersion) {
        $Csproj = Select-Xml -XPath "/" -Path "$Root\ControlR.Viewer\ControlR.Viewer.csproj"
        $AppVersion = $Csproj.Node.SelectNodes("//ApplicationVersion")
        $AppVersionInt = [int]::Parse($AppVersion[0].InnerText)
        $AppVersionInt++
        $AppVersion[0].InnerText = $AppVersionInt;
    
        $DisplayVersion = $Csproj.Node.SelectNodes("//ApplicationDisplayVersion")
        $DisplayVersionObj = [System.Version]::Parse($DisplayVersion[0].InnerText)
        $DisplayVersionObj = [System.Version]::new($DisplayVersionObj.Major, $AppVersionInt)
        $DisplayVersion[0].InnerText = $DisplayVersionObj.ToString(2)
    
        Set-Content -Path  "$Root\ControlR.Viewer\ControlR.Viewer.csproj" -Value $Csproj.Node.OuterXml.Trim()
    }

    $Manifest = Select-Xml -XPath "/" -Path "$Root\ControlR.Viewer\Platforms\Windows\Package.appxmanifest"
    $Version = [System.Version]::Parse($Manifest.Node.Package.Identity.Version)
    $NewVersion = [System.Version]::new($Version.Major, $Version.Minor, $Version.Build + 1, $Version.Revision)
    $Manifest.Node.Package.Identity.Version = $NewVersion.ToString()
    Set-Content -Path "$Root\ControlR.Viewer\Platforms\Windows\Package.appxmanifest" -Value $Manifest.Node.OuterXml.Trim()
    Remove-Item -Path "$Root\ControlR.Viewer\bin\publish\" -Force -Recurse -ErrorAction SilentlyContinue
    dotnet publish -p:PublishProfile=msix --configuration Release --framework net8.0-windows10.0.19041.0 "$Root\ControlR.Viewer\"
    New-Item -Path "$DownloadsFolder" -ItemType Directory -Force
    Get-ChildItem -Path "$Root\ControlR.Viewer\bin\publish\" -Recurse -Include "ControlR*.msix" | Select-Object -First 1 | Copy-Item -Destination "$DownloadsFolder\ControlR.Viewer.msix"
    Get-ChildItem -Path "$Root\ControlR.Viewer\bin\publish\" -Recurse -Include "ControlR*.cer" | Select-Object -First 1 | Copy-Item -Destination "$DownloadsFolder\ControlR.Viewer.cer"

    Remove-Item -Path "$Root\ControlR.Viewer\bin\publish\" -Force -Recurse -ErrorAction SilentlyContinue
    dotnet publish "$Root\ControlR.Viewer\" -f:net8.0-android -c:Release /p:AndroidSigningKeyPass=$KeystorePassword /p:AndroidSigningStorePass=$KeystorePassword -o "$Root\ControlR.Viewer\bin\publish\"
    Get-ChildItem -Path "$Root\ControlR.Viewer\bin\publish\" -Recurse -Include "*Signed.apk" | Select-Object -First 1 | Copy-Item -Destination "$DownloadsFolder\ControlR.Viewer.apk"
}

[System.IO.Directory]::CreateDirectory("$Root\ControlR.Website\public\downloads\")
Get-ChildItem -Path "$Root\ControlR.Server\wwwroot\downloads\" | Copy-Item -Destination "$Root\ControlR.Website\public\downloads\" -Recurse

Push-Location "$Root\ControlR.Website"
npm install
npm run build
Pop-Location

dotnet publish -p:ExcludeApp_Data=true --runtime ubuntu-x64 --configuration Release --output "$Root\ControlR.Server\bin\publish" --self-contained true "$Root\ControlR.Server\"