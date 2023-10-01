using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;

namespace ControlR.Agent.Services;
internal class AgentHeartbeatTimer : BackgroundService
{
    private readonly Timer _timer = new(TimeSpan.FromMinutes(5));
    private readonly IAgentHubConnection _hubConnection;
    private readonly ILogger<AgentHeartbeatTimer> _logger;

    public AgentHeartbeatTimer(
        IAgentHubConnection hubConnection,
        ILogger<AgentHeartbeatTimer> logger)
    {
        _hubConnection = hubConnection;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await _hubConnection.SendDeviceHeartbeat();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while sending agent heartbeat.");
            }
        }
    }
}
