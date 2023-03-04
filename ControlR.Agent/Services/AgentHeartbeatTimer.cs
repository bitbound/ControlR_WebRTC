using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace ControlR.Agent.Services;
internal class AgentHeartbeatTimer : IHostedService
{
    private readonly Timer _timer = new(TimeSpan.FromMinutes(5));
    private readonly IAgentHubConnection _hubConnection;

    public AgentHeartbeatTimer(IAgentHubConnection hubConnection)
    {
        _hubConnection = hubConnection;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer.Elapsed += Timer_Elapsed;
        return Task.CompletedTask;
    }

    private void Timer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        _hubConnection.SendDeviceHeartbeat();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer.Dispose();
        return Task.CompletedTask;
    }
}
