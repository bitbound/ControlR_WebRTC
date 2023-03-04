using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlR.Agent.Services.Windows;


internal class StreamerProcessWatcher : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // TODO
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
