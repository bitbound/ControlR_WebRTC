using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlR.Viewer.Models.Messages;
internal class RemoteControlDownloadProgressMessage
{
    public RemoteControlDownloadProgressMessage(Guid streamingSessionId, double downloadProgress)
    {
        StreamingSessionId = streamingSessionId;
        DownloadProgress = downloadProgress;
    }

    public Guid StreamingSessionId { get; }
    public double DownloadProgress { get; }
}
