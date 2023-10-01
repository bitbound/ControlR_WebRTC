namespace ControlR.Viewer.Models.Messages;
internal class IceCandidateMessage
{
    public IceCandidateMessage(Guid sessionId, string candidateJson)
    {
        SessionId = sessionId;
        CandidateJson = candidateJson;
    }

    public Guid SessionId { get; }
    public string CandidateJson { get; }
}
