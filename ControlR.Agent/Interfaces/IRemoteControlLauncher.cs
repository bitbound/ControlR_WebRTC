using ControlR.Agent.Models;
using ControlR.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlR.Agent.Interfaces;
internal interface IRemoteControlLauncher
{
    Task<Result> CreateSession(
     Guid sessionId,
     string authorizedKey,
     int targetWindowsSession = -1,
     string targetDesktop = "",
     Func<double, Task>? onDownloadProgress = null);
}
