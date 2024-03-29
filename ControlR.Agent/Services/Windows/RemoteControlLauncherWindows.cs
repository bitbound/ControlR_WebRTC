﻿using ControlR.Agent.Interfaces;
using ControlR.Agent.Models;
using ControlR.Agent.Models.IpcDtos;
using ControlR.Devices.Common.Native.Windows;
using ControlR.Devices.Common.Services;
using ControlR.Shared;
using ControlR.Shared.Extensions;
using ControlR.Shared.Services;
using ControlR.Shared.Services.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SimpleIpc;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.Versioning;
using Result = ControlR.Shared.Result;

namespace ControlR.Agent.Services.Windows;

[SupportedOSPlatform("windows")]
internal class RemoteControlLauncherWindows : IRemoteControlLauncher
{
    private readonly SemaphoreSlim _createSessionLock = new(1, 1);
    private readonly IDownloadsApi _downloadsApi;
    private readonly IEnvironmentHelper _environment;
    private readonly IFileSystem _fileSystem;
    private readonly IHostApplicationLifetime _hostLifetime;
    private readonly IIpcRouter _ipcRouter;
    private readonly ILogger<RemoteControlLauncherWindows> _logger;
    private readonly IProcessInvoker _processes;
    private readonly IServiceProvider _serviceProvider;
    private readonly IStreamingSessionCache _streamingSessionCache;
    private readonly string _watcherBinaryPath;

    public RemoteControlLauncherWindows(
        IFileSystem fileSystem,
        IProcessInvoker processInvoker,
        IDownloadsApi downloadsApi,
        IEnvironmentHelper environment,
        IStreamingSessionCache streamingSessionCache,
        IIpcRouter ipcRouter,
        IHostApplicationLifetime hostLifetime,
        IServiceProvider serviceProvider,
        ILogger<RemoteControlLauncherWindows> logger)
    {
        _fileSystem = fileSystem;
        _processes = processInvoker;
        _downloadsApi = downloadsApi;
        _environment = environment;
        _streamingSessionCache = streamingSessionCache;
        _ipcRouter = ipcRouter;
        _hostLifetime = hostLifetime;
        _serviceProvider = serviceProvider;
        _logger = logger;

        _watcherBinaryPath = _environment.StartupExePath;
    }

    public async Task<Result> CreateSession(
        Guid sessionId,
        byte[] authorizedKey,
        int targetWindowsSession = -1,
        string targetDesktop = "",
        Func<double, Task>? onDownloadProgress = null)
    {
        await _createSessionLock.WaitAsync();

        try
        {
            var authorizedKeyBase64 = Convert.ToBase64String(authorizedKey);

            var session = new StreamingSession(sessionId, authorizedKey, targetWindowsSession, targetDesktop);

            var watcherResult = await LaunchNewSidecarProcess(session);

            if (!watcherResult.IsSuccess)
            {
                _logger.LogResult(watcherResult);
                return Result.Fail("Failed to start desktop watcher process.");
            }

            if (_processes.GetCurrentProcess().SessionId == 0)
            {
                var startupDir = _environment.StartupDirectory;
                var remoteControlDir = Path.Combine(startupDir, "RemoteControl");
                _fileSystem.CreateDirectory(remoteControlDir);
                var binaryPath = Path.Combine(remoteControlDir, AppConstants.RemoteControlFileName);

                if (!_fileSystem.FileExists(binaryPath))
                {
                    var result = await DownloadRemoteControl(remoteControlDir, onDownloadProgress);
                    if (!result.IsSuccess)
                    {
                        return Result.Fail(result.Reason);
                    }
                }

                Win32.CreateInteractiveSystemProcess(
                    $"\"{binaryPath}\" --session-id={sessionId} --authorized-key={authorizedKeyBase64}",
                    targetSessionId: targetWindowsSession,
                    forceConsoleSession: false,
                    desktopName: session.LastDesktop,
                    hiddenWindow: false,
                    out var procInfo);

                if (procInfo.dwProcessId == -1)
                {
                    return Result.Fail("Failed to start remote control process.");
                }
                else
                {
                    _processes.GetProcessById(procInfo.dwProcessId);
                    session.StreamerProcess = _processes.GetProcessById(procInfo.dwProcessId);
                }
            }
            else
            {
                var args = $"--session-id={sessionId} --authorized-key={authorizedKeyBase64}";

                if (_environment.IsDebug)
                {
                    args += " --dev";
                }

                var solutionDirReult = GetSolutionDir(Environment.CurrentDirectory);

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
                    session.StreamerProcess = _processes.Start(psi);
                }

                if (session.StreamerProcess is null)
                {
                    return Result.Fail("Failed to start remote control process.");
                }
            }

            _streamingSessionCache.Sessions.AddOrUpdate(
               sessionId,
               session,
               (k, v) => session);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating remote control session.");
            return Result.Fail("An error occurred while starting remote control.");
        }
        finally
        {
            _createSessionLock.Release();
        }
    }

    // For debugging.
    private static Result<string> GetSolutionDir(string currentDir)
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

    private async Task<Result> LaunchNewSidecarProcess(StreamingSession session)
    {
        if (_processes.GetCurrentProcess().SessionId == 0)
        {
            var args = $"--parent-id {Environment.ProcessId} --agent-pipe \"{session.AgentPipeName}\"";
            Win32.CreateInteractiveSystemProcess(
                $"\"{_watcherBinaryPath}\" sidecar {args}",
                targetSessionId: session.TargetWindowsSession,
                forceConsoleSession: false,
                desktopName: session.LastDesktop,
                hiddenWindow: true,
                out var procInfo);

            if (procInfo.dwProcessId == -1)
            {
                _logger.LogError("Failed to start streamer process watcher.");
            }
            else
            {
                session.WatcherProcess = _processes.GetProcessById(procInfo.dwProcessId);
            }
        }
        else
        {
            var args = $"sidecar --parent-id {Environment.ProcessId} --agent-pipe \"{session.AgentPipeName}\"";
            var process = _processes.Start(_watcherBinaryPath, args);
            session.WatcherProcess = process;

            if (process is null)
            {
                _logger.LogError("Failed to start streamer process watcher.");
            }
        }

        if (session.WatcherProcess?.HasExited != false)
        {
            _logger.LogError("Watching process is unexpectedly null.");
            return Result.Fail("Watcher process failed to start.");
        }

        _logger.LogInformation("Creating pipe server for desktop watcher: {name}", session.AgentPipeName);
        session.IpcServer = await _ipcRouter.CreateServer(session.AgentPipeName);
        session.IpcServer.On<DesktopChangeDto>(async dto =>
        {
            var agentHub = _serviceProvider.GetRequiredService<IAgentHubConnection>();
            var desktopName = dto.DesktopName.Trim();

            if (!string.IsNullOrWhiteSpace(desktopName) &&
                !string.Equals(session.LastDesktop, desktopName, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation(
                    "Desktop has changed from {LastDesktop} to {CurrentDesktop}.  Notifying viewer.",
                    session.LastDesktop,
                    desktopName);

                session.LastDesktop = desktopName;
                await agentHub.NotifyViewerDesktopChanged(session.SessionId, desktopName);
            }
        });

        var result = await session.IpcServer.WaitForConnection(_hostLifetime.ApplicationStopping);
        if (result)
        {
            session.IpcServer.BeginRead(_hostLifetime.ApplicationStopping);
            _logger.LogInformation("Desktop watcher connected to pipe server.");
            var desktopResult = await session.IpcServer.Invoke<DesktopRequestDto, DesktopChangeDto>(new());
            if (desktopResult.IsSuccess)
            {
                session.LastDesktop = desktopResult.Value.DesktopName;
                return Result.Ok();
            }
            _logger.LogError("Failed to get initial desktop from watcher.");
            return Result.Fail(desktopResult.Error);
        }
        else
        {
            _logger.LogWarning("Desktop watcher failed to connect to pipe server.");
            return Result.Fail("Desktop watcher failed to connect to pipe server.");
        }
    }
}