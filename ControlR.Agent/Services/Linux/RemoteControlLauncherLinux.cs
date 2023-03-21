using ControlR.Agent.Interfaces;
using ControlR.Agent.Models;
using ControlR.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace ControlR.Agent.Services.Linux;

[SupportedOSPlatform("linux")]
internal class RemoteControlLauncherLinux : IRemoteControlLauncher
{
    public Task<Result> CreateSession(Guid sessionId, string authorizedKey, int targetWindowsSession = -1, string targetDesktop = "", Func<double, Task>? onDownloadProgress = null)
    {
        throw new NotImplementedException();
    }
}
