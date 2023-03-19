using ControlR.Devices.Common.Native.Windows;
using ControlR.Devices.Common.Services;
using ControlR.Shared;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace ControlR.Agent.Services.Windows;

internal interface IInputDesktopReporter
{
    Task Start(int streamerProcessId, int parentProcessId);
}

[SupportedOSPlatform("windows")]
internal class InputDesktopReporter : IInputDesktopReporter
{
    private readonly IHostApplicationLifetime _hostLifetime;
    private readonly IProcessInvoker _processes;
    private readonly ILogger<InputDesktopReporter> _logger;
    private int _streamerId = -1;
    private int _parentId;
    private string _lastDesktop = "Default";

    public InputDesktopReporter(
        IHostApplicationLifetime hostLifetime,
        IProcessInvoker processes,
        ILogger<InputDesktopReporter> logger)
    {
        _hostLifetime = hostLifetime;
        _processes = processes;
        _logger = logger;
    }

    public Task Start(int streamerId, int parentProcessId)
    {
        _streamerId = streamerId;
        _parentId = parentProcessId;
        _ = Task.Run(WatchDesktop);

        return Task.CompletedTask;
    }

    private async Task WatchDesktop()
    {
        if (_streamerId == -1)
        {
            _logger.LogError("Streamer ID shouldn't be -1 here.");
            _hostLifetime.StopApplication();
            return;
        }

        if (_parentId == -1)
        {
            _logger.LogError("Parent process ID shouldn't be -1 here.");
            _hostLifetime.StopApplication();
            return;
        }

        _logger.LogInformation("Beginning desktop watch for streamer ID {id}.", _streamerId);

        var mmfName = AppConstants.GetDesktopWatcherMmfName(_streamerId, Environment.ProcessId);
        using var mmf = MemoryMappedFile.CreateOrOpen(mmfName, 64);
        using var accessor = mmf.CreateViewStream();
        

        while (!_hostLifetime.ApplicationStopping.IsCancellationRequested)
        {
            try
            {
                var processes = _processes
                    .GetProcesses()
                    .ToDictionary(x => x.Id, x => x);

                if (!processes.ContainsKey(_streamerId))
                {
                    _logger.LogInformation("Streamer ID {id} no longer exists.  Exiting watcher process.", _streamerId);
                    _hostLifetime.StopApplication();
                    return;
                }

                if (!processes.ContainsKey(_parentId))
                {
                    _logger.LogInformation("Parent ID {id} no longer exists.  Exiting watcher process.", _parentId);
                    _hostLifetime.StopApplication();
                    return;
                }

                if (!Win32.GetCurrentDesktop(out var desktopName))
                {
                    _logger.LogError("Failed to get current desktop.");
                }

                if (!string.IsNullOrWhiteSpace(desktopName) &&
                    !string.Equals(_lastDesktop, desktopName, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Desktop has changed from {last} to {current}. Reporting to streamer.", _lastDesktop, desktopName);
                    accessor.Seek(0, SeekOrigin.Begin);
                    using var sw = new StreamWriter(accessor, Encoding.UTF8, leaveOpen: true);
                    await sw.WriteLineAsync(desktopName);
                    _lastDesktop = desktopName;
                }

                await Task.Delay(50);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while reporting input desktop.");
            }
        }
    }
}
