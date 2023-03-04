using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlR.Viewer.Models.Messages;
internal class RemoteControlDownloadProgressMessage
{
    public RemoteControlDownloadProgressMessage(Guid desktopSessionId, double downloadProgress)
    {
        DesktopSessionId = desktopSessionId;
        DownloadProgress = downloadProgress;
    }

    public Guid DesktopSessionId { get; }
    public double DownloadProgress { get; }
}
