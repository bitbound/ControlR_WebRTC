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
    private readonly IProcessInvoker _processes;
    private readonly IDownloadsApi _downloadsApi;
    private readonly IEnvironmentHelper _environmentHelper;
    private readonly IStreamingSessionCache _streamingSessionCache;
    private readonly ILogger<RemoteControlLauncherWindows> _logger;

    public RemoteControlLauncherWindows(
        IFileSystem fileSystem,
        IProcessInvoker processInvoker,
        IDownloadsApi downloadsApi,
        IEnvironmentHelper environmentHelper,
        IStreamingSessionCache streamingSessionCache,
        ILogger<RemoteControlLauncherWindows> logger)
    {
        _fileSystem = fileSystem;
        _processes = processInvoker;
        _downloadsApi = downloadsApi;
        _environmentHelper = environmentHelper;
        _streamingSessionCache = streamingSessionCache;
        _logger = logger;
    }

    public async Task<Result> CreateSession(
        Guid sessionId, 
        int targetWindowsSession,
        string authorizedKey,
        Func<double, Task>? onDownloadProgress)
    {
        var result = await CreateSessionImpl(sessionId, targetWindowsSession, authorizedKey, "Default", onDownloadProgress);

        if (!result.IsSuccess)
        {
            return Result.Fail(result.Reason);
        }
 
        var session = new StreamingSession(result.Value, sessionId, authorizedKey);
        _streamingSessionCache.Sessions.AddOrUpdate(
            sessionId,
            session,
            (k, v) => session);

        return Result.Ok();
    }

    public async Task<Result> RelaunchInNewDesktop(StreamingSession session, string desktopName, int targetWindowsSession)
    {
        var result = await CreateSessionImpl(
            session.SessionId, 
            targetWindowsSession, 
            session.AuthorizedKey, 
            desktopName, 
            null);

        if (!result.IsSuccess)
        {
            return Result.Fail(result.Reason);
        }

        var oldStreamer = session.StreamerProcess;
        session.LastDesktop = desktopName;
        session.StreamerProcess = result.Value;
        _streamingSessionCache.Sessions.AddOrUpdate(
          session.SessionId,
          session,
          (k, v) => session);

        oldStreamer.Kill();

        return Result.Ok();
    }

    private async Task<Result<Process>> CreateSessionImpl(
      Guid sessionId,
      int targetWindowsSession,
      string authorizedKey,
      string targetDesktop,
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
                return Result.Fail<Process>(result.Reason);
            }
        }

        if (_processes.GetCurrentProcess().SessionId == 0)
        {
            Win32.CreateInteractiveSystemProcess(
                $"\"{binaryPath}\" --session-id={sessionId} --authorized-key={authorizedKey}",
                targetSessionId: targetWindowsSession,
                forceConsoleSession: false,
                desktopName: targetDesktop,
                hiddenWindow: false,
                out var procInfo);


            if (procInfo.dwProcessId == -1)
            {
                return Result.Fail<Process>("Failed to start remote control process.");
            }
            else
            {
                _processes.GetProcessById(procInfo.dwProcessId);
                var process = _processes.GetProcessById(procInfo.dwProcessId);
                return Result.Ok(process);
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
                process = _processes.Start(psi);
            }
            else
            {
                process = _processes.Start(binaryPath, args);
            }

            if (process is null)
            {
                return Result.Fail<Process>("Failed to start remote control process.");
            }

            return Result.Ok(process);
        }
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
