using ControlR.Agent.Interfaces;
using ControlR.Agent.Models;
using ControlR.Devices.Common.Native.Windows;
using ControlR.Devices.Common.Services;
using ControlR.Shared;
using ControlR.Shared.Services;
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
using Timer = System.Timers.Timer;

namespace ControlR.Agent.Services.Windows;

[SupportedOSPlatform("windows")]
internal class StreamingSessionWatcher : IHostedService
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly Timer _timer = new(50);
    private IStreamingSessionCache _cache;
    private readonly IProcessInvoker _processes;
    private readonly ILogger<StreamingSessionWatcher> _logger;
    private readonly IEnvironmentHelper _environment;
    private readonly IRemoteControlLauncher _remoteControlLauncher;
    private readonly string _watcherBinaryPath;

    public StreamingSessionWatcher(
        IStreamingSessionCache streamerCache,
        IProcessInvoker processes,
        IEnvironmentHelper environmentHelper,
        IRemoteControlLauncher remoteControlLauncher,
        ILogger<StreamingSessionWatcher> logger)
    {
        _cache = streamerCache;
        _processes = processes;
        _environment = environmentHelper;
        _remoteControlLauncher = remoteControlLauncher;
        _logger = logger;

        _watcherBinaryPath = _environment.StartupExePath;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer.Elapsed += Timer_Elapsed;
        _timer.Start();
        return Task.CompletedTask;
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
                    _cache.Streamers.TryRemove(session.StreamerProcessId, out _);
                    continue;
                }

                if (!EnsureActiveStreamer(processes, session))
                {
                    continue;
                }

                var mmfName = AppConstants.GetDesktopWatcherMmfName(session.StreamerProcessId, session.WatcherProcessId);
                using var mmf = MemoryMappedFile.CreateOrOpen(mmfName, 64);
                using var accessor = mmf.CreateViewStream();
                using var sr = new StreamReader(accessor, Encoding.UTF8);
                var desktop = await sr.ReadLineAsync() ?? string.Empty;

                desktop = desktop.Replace("\0", string.Empty);

                if (!string.IsNullOrWhiteSpace(desktop) &&
                    !string.Equals(session.LastDesktop, desktop, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Desktop has changed from {last} to {current}.  Relaunching streamer.", session.LastDesktop, desktop);
                    session.LastDesktop = desktop;
                    await _remoteControlLauncher.CreateSession(session.SessionId, streamerProcess.SessionId, session.AuthorizedKey, null);
                    streamerProcess.Kill();
                }
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

    private bool EnsureActiveStreamer(Dictionary<int, Process> processes, StreamingSession value)
    {
        try
        {
            if (!processes.TryGetValue(value.StreamerProcessId, out var streamerProcess))
            {
                _cache.Streamers.TryRemove(value.StreamerProcessId, out _);

                if (processes.TryGetValue(value.WatcherProcessId, out var watcherProcess))
                {
                    watcherProcess.Kill();
                }
                return false;
            }

            if (!processes.ContainsKey(value.WatcherProcessId))
            {
                LaunchNewWatcherProcess(value, streamerProcess);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while ensuring active stream for streamer process {id}.", value.StreamerProcessId);
            return false;
        }
    }

    private void LaunchNewWatcherProcess(StreamingSession streamer, Process streamerProcess)
    {
        if (streamer.WatcherProcessId == -1)
        {
            _logger.LogInformation("Starting new watcher for streamer {id}.", streamer.StreamerProcessId);
        }
        else
        {
            _logger.LogWarning("Restarting watcher for streamer {id}.", streamer.StreamerProcessId);
        }

        if (_processes.GetCurrentProcess().SessionId == 0)
        {
            Win32.CreateInteractiveSystemProcess(
                $"\"{_watcherBinaryPath}\" watch-desktop --streamer-id {streamer.StreamerProcessId} --parent-id {Environment.ProcessId}",
                targetSessionId: streamerProcess.SessionId,
                forceConsoleSession: false,
                desktopName: "Default",
                hiddenWindow: true,
                out var procInfo);

            streamer.WatcherProcessId = procInfo.dwProcessId;

            if (procInfo.dwProcessId == -1)
            {
                _logger.LogError("Failed to start streamer process watcher.");
            }
        }
        else
        {
            var process = _processes.Start(_watcherBinaryPath, $"watch-desktop --streamer-id {streamerProcess.Id} --parent-id {Environment.ProcessId}");
            streamer.WatcherProcessId = process?.Id ?? -1;

            if (process is null)
            {
                _logger.LogError("Failed to start streamer process watcher.");
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer.Dispose();
        return Task.CompletedTask;
    }
}
