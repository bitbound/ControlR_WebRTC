using ControlR.Agent.Models.IpcDtos;
using ControlR.Devices.Common.Native.Windows;
using ControlR.Devices.Common.Services;
using ControlR.Shared;
using EasyIpc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PInvoke;
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
    Task Start(string agentPipeName, int parentProcessId);
}

[SupportedOSPlatform("windows")]
internal class InputDesktopReporter : IInputDesktopReporter
{
    private readonly IHostApplicationLifetime _hostLifetime;
    private readonly IProcessInvoker _processes;
    private readonly IIpcConnectionFactory _ipcFactory;
    private readonly ILogger<InputDesktopReporter> _logger;
    private string _agentPipeName = string.Empty;
    private int _parentId;
    private string _lastDesktop = "Default";

    public InputDesktopReporter(
        IHostApplicationLifetime hostLifetime,
        IProcessInvoker processes,
        IIpcConnectionFactory ipcFactory,
        ILogger<InputDesktopReporter> logger)
    {
        _hostLifetime = hostLifetime;
        _processes = processes;
        _ipcFactory = ipcFactory;
        _logger = logger;
    }

    public Task Start(string agentPipeName, int parentProcessId)
    {
        _agentPipeName = agentPipeName;
        _parentId = parentProcessId;
        _ = Task.Run(WatchDesktop);

        return Task.CompletedTask;
    }

    private async Task WatchDesktop()
    {
        if (string.IsNullOrWhiteSpace(_agentPipeName))
        {
            _logger.LogError("Agent pipe name cannot be null.");
            _hostLifetime.StopApplication();
            return;
        }

        if (_parentId == -1)
        {
            _logger.LogError("Parent process ID shouldn't be -1 here.");
            _hostLifetime.StopApplication();
            return;
        }

        var parentProcess = _processes.GetProcessById(_parentId);

        _logger.LogInformation("Beginning desktop watch for pipe: ", _agentPipeName);

     
        if (Win32.GetThreadDesktop((uint)Environment.CurrentManagedThreadId, out var currentDesktop))
        {
            _lastDesktop = currentDesktop;
            _logger.LogInformation("Initial desktop: {desktopName}", currentDesktop);
        }
        else
        {
            _logger.LogWarning("Failed to get initial desktop.");
        }

        _logger.LogInformation("Creating IPC client for pipe name: {name}", _agentPipeName);
        var client = await _ipcFactory.CreateClient(".", _agentPipeName);
        var connected = await client.Connect(10_000);

        if (!connected)
        {
            _logger.LogError("Failed to connect to pipe server host to send desktop change updates.");
            return;
        }

        client.BeginRead(_hostLifetime.ApplicationStopping);

        _logger.LogInformation("Connected to pipe server.");

        _logger.LogInformation("Sending initial desktop.");

        await client.Send(new DesktopChangeDto(_lastDesktop));

        while (!_hostLifetime.ApplicationStopping.IsCancellationRequested && client.IsConnected)
        {
            try
            {
                if (parentProcess.HasExited)
                {
                    _logger.LogInformation("Parent ID {id} no longer exists.  Exiting watcher process.", _parentId);
                    _hostLifetime.StopApplication();
                    return;
                }

                if (!Win32.GetInputDesktop(out var desktopName))
                {
                    _logger.LogError("Failed to get current desktop.");
                }

                if (!string.IsNullOrWhiteSpace(desktopName) &&
                    !string.Equals(_lastDesktop, desktopName, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("Desktop has changed from {last} to {current}.  Sending to agent.", _lastDesktop, desktopName);
                    await client.Send(new DesktopChangeDto(desktopName));
                    _lastDesktop = desktopName;
                    if (!Win32.SwitchToInputDesktop())
                    {
                        _logger.LogWarning("Failed to switch to input desktop.");
                    }
                }

                await Task.Delay(50);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while reporting input desktop.");
            }
        }

        _logger.LogInformation("Exiting desktop watch for pipe name: {name}", _agentPipeName);
    }
}
