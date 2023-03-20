using ControlR.Agent.Interfaces;
using ControlR.Agent.Models;
using ControlR.Agent.Models.IpcDtos;
using ControlR.Devices.Common.Native.Windows;
using ControlR.Devices.Common.Services;
using ControlR.Shared;
using ControlR.Shared.Services;
using EasyIpc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using static System.Collections.Specialized.BitVector32;
using Timer = System.Timers.Timer;

namespace ControlR.Agent.Services.Windows;

[SupportedOSPlatform("windows")]
internal class StreamingSessionWatcher : IHostedService
{
    private readonly IEnvironmentHelper _environment;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly ILogger<StreamingSessionWatcher> _logger;
    private readonly IProcessInvoker _processes;
    private readonly IRemoteControlLauncher _remoteControlLauncher;
    private readonly IHostApplicationLifetime _hostLifetime;
    private readonly IIpcRouter _ipcRouter;
    private readonly Timer _timer = new(50);
    private readonly string _watcherBinaryPath;
    private readonly IStreamingSessionCache _cache;

    public StreamingSessionWatcher(
        IStreamingSessionCache streamerCache,
        IProcessInvoker processes,
        IEnvironmentHelper environmentHelper,
        IRemoteControlLauncher remoteControlLauncher,
        IHostApplicationLifetime hostLifetime,
        IIpcRouter ipcRouter,
        ILogger<StreamingSessionWatcher> logger)
    {
        _cache = streamerCache;
        _processes = processes;
        _environment = environmentHelper;
        _remoteControlLauncher = remoteControlLauncher;
        _hostLifetime = hostLifetime;
        _ipcRouter = ipcRouter;
        _logger = logger;

        _watcherBinaryPath = _environment.StartupExePath;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer.Elapsed += Timer_Elapsed;
        _timer.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer.Dispose();
        return Task.CompletedTask;
    }

    private async Task EnsureActiveSession(StreamingSession session)
    {
        try
        {
            if (session.StreamerProcess.HasExited)
            {
                _cache.Sessions.TryRemove(session.SessionId, out _);
                session.Dispose();
                return;
            }

            if (session.WatcherProcess?.HasExited != false)
            {
                await LaunchNewWatcherProcess(session);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while ensuring active stream for streamer process {id}.", session.StreamerProcess.Id);
        }
    }

    private async Task LaunchNewWatcherProcess(StreamingSession session)
    {
        if (session.WatcherProcess is null)
        {
            _logger.LogInformation("Starting new watcher for streamer {id}.", session.StreamerProcess.Id);
        }
        else
        {
            _logger.LogWarning("Restarting watcher for streamer {id}.", session.StreamerProcess.Id);
        }

        if (_processes.GetCurrentProcess().SessionId == 0)
        {
            var args = $"--parent-id {Environment.ProcessId} --agent-pipe \"{session.AgentPipeName}\"";
            Win32.CreateInteractiveSystemProcess(
                $"\"{_watcherBinaryPath}\" watch-desktop {args}",
                targetSessionId: session.StreamerProcess.SessionId,
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
            var args = $"watch-desktop --parent-id {Environment.ProcessId} --agent-pipe \"{session.AgentPipeName}\"";
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
            return;
        }

        _logger.LogInformation("Creating pipe server for desktop watcher: {name}", session.AgentPipeName);
        session.IpcServer = await _ipcRouter.CreateServer(session.AgentPipeName);
        session.IpcServer.On<DesktopChangeDto>(async dto =>
        {
            await _lock.WaitAsync();
            try
            {
                var desktopName = dto.DesktopName.Trim();

                if (!string.IsNullOrWhiteSpace(desktopName) &&
                    !string.Equals(session.LastDesktop, desktopName, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Desktop has changed from {last} to {current}.  Relaunching streamer.", session.LastDesktop, desktopName);
                    await _remoteControlLauncher.RelaunchInNewDesktop(session, desktopName, session.StreamerProcess.SessionId);
                }
            }
            finally
            {
                _lock.Release();
            }
        });

        var result = await session.IpcServer.WaitForConnection(_hostLifetime.ApplicationStopping);
        if (result)
        {
            session.IpcServer.BeginRead(_hostLifetime.ApplicationStopping);
            _logger.LogInformation("Client connected to pipe server.");
        }
        else
        {
            _logger.LogWarning("Client failed to connect to pipe server.");
        }
    }

    private async void Timer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        if (!await _lock.WaitAsync(0))
        {
            return;
        }

        try
        {
            foreach (var kvp in _cache.Sessions)
            {
                var session = kvp.Value;

                if (session.StreamerProcess.HasExited)
                {
                    if (_cache.Sessions.TryRemove(session.SessionId, out var oldSession))
                    {
                        oldSession.Dispose();
                    }
                    continue;
                }

                await EnsureActiveSession(session);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while checking streamer processes.");
        }
        finally
        {
            _lock.Release();
        }
    }
}
