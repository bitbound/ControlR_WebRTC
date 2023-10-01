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
