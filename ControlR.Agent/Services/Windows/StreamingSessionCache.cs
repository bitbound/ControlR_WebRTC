using ControlR.Agent.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlR.Agent.Services.Windows;

internal interface IStreamingSessionCache
{
    ConcurrentDictionary<int, StreamingSession> Streamers { get; }
}
internal class StreamingSessionCache : IStreamingSessionCache
{
    public ConcurrentDictionary<int, StreamingSession> Streamers { get; } = new();

}
