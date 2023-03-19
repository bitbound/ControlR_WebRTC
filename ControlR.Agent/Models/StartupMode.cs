using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlR.Agent.Models;
internal enum StartupMode
{
    None,
    Run,
    Install,
    Uninstall,
    WatchDesktop
}
