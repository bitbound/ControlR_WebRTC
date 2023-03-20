using ControlR.Agent.Models;
using ControlR.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlR.Agent.Interfaces;
internal interface IRemoteControlLauncher
{
    Task<Result> CreateSession(
        Guid sessionId, 
        int targetSystemSession, 
        string authorizedKey, 
        Func<double, Task>? onDownloadProgress);

    Task<Result> RelaunchInNewDesktop(StreamingSession session, string desktopName, int targetWindowsSession);
}
