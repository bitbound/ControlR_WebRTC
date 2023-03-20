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
    public Task<Result> CreateSession(Guid sessionId, int targetSystemSession, string authorizedKey, Func<double, Task>? onDownloadProgress)
    {
        throw new NotImplementedException();
    }

    public Task<Result> RelaunchInNewDesktop(StreamingSession session, string desktopName, int targetWindowsSession)
    {
        throw new NotImplementedException();
    }
}
