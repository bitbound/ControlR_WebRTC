using ControlR.Agent.Interfaces;
using ControlR.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace ControlR.Agent.Services.Linux;

[SupportedOSPlatform("linux")]
public class RemoteControlLauncherLinux : IRemoteControlLauncher
{
    public Task<Result> CreateSession(
        Guid sessionId, 
        int targetSystemSession,
        string authorizedKey,
        Func<double, Task>? onDownloadProgress)
    {
        // TODO
        throw new NotImplementedException();
    }
}
