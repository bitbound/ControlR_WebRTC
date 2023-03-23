param (
    [Parameter(Mandatory = $true)]
    [string]$CertificatePath,

    [Parameter(Mandatory=$true)]
    [string]$CertificatePassword,

    [Parameter(Mandatory=$true)]
    [string]$SignToolPath,

    [switch]$BuildAgent,

    [switch]$BuildViewer,

    [switch]$BuildStreamer
)

$InstallerDir = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer"
$VsWhere = "$InstallerDir\vswhere.exe"
$MSBuildPath = (&"$VsWhere" -latest -products * -find "\MSBuild\Current\Bin\MSBuild.exe").Trim()
$Root = (Get-Item -Path $PSScriptRoot).Parent.FullName
$DownloadsFolder = "$Root\ControlR.Server\wwwroot\downloads\"
$CurrentVersion = [System.DateTime]::Now.ToString("yyyy.MM.dd.HHmm")

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
    Start-Sleep -Seconds 1
    &"$SignToolPath" sign /fd SHA256 /f "$CertificatePath" /p $CertificatePassword /t http://timestamp.digicert.com "$Root\ControlR.Server\wwwroot\downloads\ControlR.Agent.exe"
}

if ($BuildStreamer) {
    [string]$PackageJson = Get-Content -Path "$Root\ControlR.Streamer\package.json"
    $Package = $PackageJson | ConvertFrom-Json
    $Package.version = $CurrentVersion
    [string]$PackageJson = $Package | ConvertTo-Json
    [System.IO.File]::WriteAllText("$Root\ControlR.Streamer\package.json", $PackageJson)
    Push-Location "$Root\ControlR.Streamer"
    npm run make-pwsh
    Pop-Location
}

if ($BuildViewer) {
    Remove-Item -Path "$Root\ControlR.Viewer\bin\publish\" -Force -Recurse -ErrorAction SilentlyContinue
    dotnet publish -p:PublishProfile=msix --configuration Release --framework net7.0-windows10.0.19041.0 "$Root\ControlR.Viewer\"
    New-Item -Path "$Root\ControlR.Server\wwwroot\downloads\" -ItemType Directory -Force
    Get-ChildItem -Path "$Root\ControlR.Viewer\bin\publish\" -Recurse -Include "ControlR*.msix" | Select-Object -First 1 | Copy-Item -Destination "$Root\ControlR.Server\wwwroot\downloads\ControlR.Viewer.msix"
    Get-ChildItem -Path "$Root\ControlR.Viewer\bin\publish\" -Recurse -Include "ControlR*.cer" | Select-Object -First 1 | Copy-Item -Destination "$Root\ControlR.Server\wwwroot\downloads\ControlR.Viewer.cer"
}

dotnet publish -p:ExcludeApp_Data=true --runtime ubuntu-x64 --configuration Release --output "$Root\ControlR.Server\bin\publish" --self-contained true "$Root\ControlR.Server\"