using ControlR.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlR.Agent.Interfaces;
internal interface IPowerControl
{
    Task ChangeState(PowerStateChangeType type);
}
