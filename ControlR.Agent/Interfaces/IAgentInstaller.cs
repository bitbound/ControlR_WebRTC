using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlR.Agent.Interfaces;

public interface IAgentInstaller
{
    Task Install(string? authorizedPublicKey = null);
    Task Uninstall();
}
