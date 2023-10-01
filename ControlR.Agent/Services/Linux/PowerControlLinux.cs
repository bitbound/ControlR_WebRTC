using ControlR.Agent.Interfaces;
using ControlR.Shared.Enums;

namespace ControlR.Agent.Services.Linux;
internal class PowerControlLinux : IPowerControl
{
    public Task ChangeState(PowerStateChangeType type)
    {
        throw new NotImplementedException();
    }
}
