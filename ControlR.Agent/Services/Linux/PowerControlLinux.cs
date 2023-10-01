using ControlR.Agent.Interfaces;
using ControlR.Devices.Common.Services;
using ControlR.Shared.Enums;

namespace ControlR.Agent.Services.Linux;
internal class PowerControlLinux : IPowerControl
{
    private readonly IProcessInvoker _processInvoker;

    public PowerControlLinux(IProcessInvoker processInvoker)
    {
        _processInvoker = processInvoker;
    }

    public Task ChangeState(PowerStateChangeType type)
    {
        switch (type)
        {
            case PowerStateChangeType.Restart:
                {
                    _ = _processInvoker.Start("shutdown", "-r 0", true);
                }
                break;
            case PowerStateChangeType.Shutdown:
                {
                    _ = _processInvoker.Start("shutdown", "-p 0", true);
                }
                break;
            default:
                break;
        }
        return Task.CompletedTask;
    }
}
