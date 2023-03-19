using ControlR.Agent.Interfaces;
using ControlR.Agent.Models;
using ControlR.Devices.Common.Native.Windows;
using ControlR.Devices.Common.Services;
using ControlR.Shared;
using ControlR.Shared.Services;
using ControlR.Shared.Services.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace ControlR.Agent.Services.Windows;

[SupportedOSPlatform("windows")]
internal class RemoteControlLauncherWindows : IRemoteControlLauncher
{
    private readonly IFileSystem _fileSystem;
    private readonly IProcessInvoker _processInvoker;
    private readonly IDownloadsApi _downloadsApi;
    private readonly IEnvironmentHelper _environmentHelper;
    private readonly IStreamingSessionCache _streamerProcessCache;
    private readonly ILogger<RemoteControlLauncherWindows> _logger;

    public RemoteControlLauncherWindows(
        IFileSystem fileSystem,
        IProcessInvoker processInvoker,
        IDownloadsApi downloadsApi,
        IEnvironmentHelper environmentHelper,
        IStreamingSessionCache streamerProcessCache,
        ILogger<RemoteControlLauncherWindows> logger)
    {
        _fileSystem = fileSystem;
        _processInvoker = processInvoker;
        _downloadsApi = downloadsApi;
        _environmentHelper = environmentHelper;
        _streamerProcessCache = streamerProcessCache;
        _logger = logger;
    }

    public async Task<Result> CreateSession(
        Guid sessionId, 
        int targetWindowsSession,
        string authorizedKey,
        Func<double, Task>? onDownloadProgress)
    {
        var startupDir = _environmentHelper.StartupDirectory;

        var remoteControlDir = Path.Combine(startupDir, "RemoteControl");
        _fileSystem.CreateDirectory(remoteControlDir);

        var binaryPath = Path.Combine(remoteControlDir, AppConstants.RemoteControlFileName);

        if (!_fileSystem.FileExists(binaryPath))
        {
            var result = await DownloadRemoteControl(remoteControlDir, onDownloadProgress);
            if (!result.IsSuccess)
            {
                return result;
            }
        }

        if (_processInvoker.GetCurrentProcess().SessionId == 0)
        {
            Win32.CreateInteractiveSystemProcess(
                $"\"{binaryPath}\" --session-id={sessionId} --authorized-key={authorizedKey}",
                targetSessionId: targetWindowsSession, 
                forceConsoleSession: false,
                desktopName: "Default", 
                hiddenWindow: false,
                out var procInfo);

            
            if (procInfo.dwProcessId == -1)
            {
                return Result.Fail("Failed to start remote control process.");
            }
            else
            {
                var session = new StreamingSession(procInfo.dwProcessId, sessionId, authorizedKey);
                _streamerProcessCache.Streamers.AddOrUpdate(
                    procInfo.dwProcessId,
                    session,
                    (k,v) => session);
            }
            
        }
        else
        {
            var args = $"--session-id={sessionId} --authorized-key={authorizedKey}";

            if (_environmentHelper.IsDebug)
            {
                args += " --dev";
            }

            var solutionDirReult = GetSolutionDir(Environment.CurrentDirectory);

            Process? process;

            if (solutionDirReult.IsSuccess)
            {
                var desktopDir = Path.Combine(solutionDirReult.Value, "ControlR.Streamer");
                var psi = new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    Arguments = $"/k npm run start -- -- {args}",
                    WorkingDirectory = desktopDir,
                    UseShellExecute = true
                };
                process = _processInvoker.Start(psi);
            }
            else
            {
                process = _processInvoker.Start(binaryPath, args);
            }

            if (process is null)
            {
                return Result.Fail("Failed to start remote control process.");
            }
            else
            {
                var session = new StreamingSession(process.Id, sessionId, authorizedKey);
                _streamerProcessCache.Streamers.AddOrUpdate(
                    process.Id,
                    session,
                    (k, v) => session);
            }
        }

        return Result.Ok();
    }

    private async Task<Result> DownloadRemoteControl(string remoteControlDir, Func<double, Task>? onDownloadProgress)
    {
        try
        {
            var targetPath = Path.Combine(remoteControlDir, AppConstants.RemoteControlZipFileName);
            var result = await _downloadsApi.DownloadRemoteControlZip(targetPath, onDownloadProgress);
            if (!result.IsSuccess)
            {
                return result;
            }
            ZipFile.ExtractToDirectory(targetPath, remoteControlDir);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while extracting remote control archive.");
            return Result.Fail(ex);
        }
    }

    // For debugging.
    private Result<string> GetSolutionDir(string currentDir)
    {
        var dirInfo = new DirectoryInfo(currentDir);
        if (!dirInfo.Exists)
        {
            return Result.Fail<string>("Not found.");
        }

        if (dirInfo.GetFiles().Any(x => x.Name == "ControlR.sln"))
        {
            return Result.Ok(currentDir);
        }

        if (dirInfo.Parent is not null)
        {
            return GetSolutionDir(dirInfo.Parent.FullName);
        }

        return Result.Fail<string>("Not found.");
    }


}
