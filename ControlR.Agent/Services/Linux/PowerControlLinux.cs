using ControlR.Agent.Interfaces;
using ControlR.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlR.Agent.Services.Linux;
internal class PowerControlLinux : IPowerControl
{
    public Task ChangeState(PowerStateChangeType type)
    {
        throw new NotImplementedException();
    }
}
