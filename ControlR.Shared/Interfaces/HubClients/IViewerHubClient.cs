using ControlR.Shared.DbEntities;
using ControlR.Shared.Dtos;
using ControlR.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlR.Shared.Interfaces.HubClients;
public interface IViewerHubClient : IHubClient
{
    Task ReceiveDeviceUpdate(DeviceDto device);
    Task ReceiveIceCandidate(Guid sessionId, string candidateJson);
    Task ReceiveRemoteControlDownloadProgress(Guid desktopSessionId, double downloadProgress);
    Task ReceiveRtcSessionDescription(Guid sessionId, RtcSessionDescription sessionDescription);
}
