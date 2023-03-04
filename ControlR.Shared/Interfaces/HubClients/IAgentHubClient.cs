using ControlR.Shared.Dtos;
using ControlR.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlR.Shared.Interfaces.HubClients;
public interface IAgentHubClient : IHubClient
{
    Task<bool> GetDesktopSession(SignedPayloadDto sessionRequest);
    Task<WindowsSession[]> GetWindowsSessions(SignedPayloadDto signedDto);
}
