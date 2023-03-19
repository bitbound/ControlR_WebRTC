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

    private async Task EnsureActiveSession(Dictionary<int, Process> processes, StreamingSession value)
    {
        try
        {
            if (!processes.TryGetValue(value.StreamerProcessId, out var streamerProcess))
            {
                if (_cache.Streamers.TryRemove(value.StreamerProcessId, out var oldSession))
                {
                    oldSession.Dispose();
                }

                if (processes.TryGetValue(value.WatcherProcessId, out var watcherProcess))
                {
                    watcherProcess.Kill();
                }
                return;
            }

            if (!processes.ContainsKey(value.WatcherProcessId))
            {
                await LaunchNewWatcherProcess(value, streamerProcess);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while ensuring active stream for streamer process {id}.", value.StreamerProcessId);
        }
    }

    private async Task LaunchNewWatcherProcess(StreamingSession session, Process streamerProcess)
    {
        if (session.WatcherProcessId == -1)
        {
            _logger.LogInformation("Starting new watcher for streamer {id}.", session.StreamerProcessId);
        }
        else
        {
            _logger.LogWarning("Restarting watcher for streamer {id}.", session.StreamerProcessId);
        }

        if (_processes.GetCurrentProcess().SessionId == 0)
        {
            Win32.CreateInteractiveSystemProcess(
                $"\"{_watcherBinaryPath}\" watch-desktop --streamer-id {session.StreamerProcessId} --parent-id {Environment.ProcessId}",
                targetSessionId: streamerProcess.SessionId,
                forceConsoleSession: false,
                desktopName: "Default",
                hiddenWindow: true,
                out var procInfo);

            session.WatcherProcessId = procInfo.dwProcessId;

            if (procInfo.dwProcessId == -1)
            {
                _logger.LogError("Failed to start streamer process watcher.");
            }
        }
        else
        {
            var process = _processes.Start(_watcherBinaryPath, $"watch-desktop --streamer-id {streamerProcess.Id} --parent-id {Environment.ProcessId}");
            session.WatcherProcessId = process?.Id ?? -1;

            if (process is null)
            {
                _logger.LogError("Failed to start streamer process watcher.");
            }
        }

        if (session.WatcherProcessId > -1)
        {
            var pipeName = AppConstants.GetDesktopWatcherPipeName(session.StreamerProcessId, session.WatcherProcessId);
            _logger.LogInformation("Creating pipe server for desktop watcher: {name}", pipeName);
            session.IpcServer = await _ipcRouter.CreateServer(pipeName);
            session.IpcServer.On<DesktopChangeDto>(async dto =>
            {
                var desktop = dto.DesktopName.Replace("\0", string.Empty);

                if (!string.IsNullOrWhiteSpace(desktop) &&
                    !string.Equals(session.LastDesktop, desktop, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Desktop has changed from {last} to {current}.  Relaunching streamer.", session.LastDesktop, desktop);
                    session.LastDesktop = desktop;
                    await _remoteControlLauncher.CreateSession(session.SessionId, streamerProcess.SessionId, session.AuthorizedKey, null);
                    streamerProcess.Kill();
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
    }

    private async void Timer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        if (!await _lock.WaitAsync(0))
        {
            return;
        }

        try
        {
            var processes = _processes
                .GetProcesses()
                .ToDictionary(x => x.Id, x => x);

            foreach (var kvp in _cache.Streamers)
            {
                var session = kvp.Value;

                if (!processes.TryGetValue(session.StreamerProcessId, out var streamerProcess))
                {
                    if (_cache.Streamers.TryRemove(session.StreamerProcessId, out var oldSession))
                    {
                        oldSession.Dispose();
                    }
                    continue;
                }

                await EnsureActiveSession(processes, session);
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
