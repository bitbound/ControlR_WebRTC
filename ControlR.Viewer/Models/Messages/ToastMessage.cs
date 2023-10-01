using MudBlazor;

namespace ControlR.Viewer.Models.Messages;
internal class ToastMessage
{
    public ToastMessage(string message, Severity severity)
    {
        Message = message;
        Severity = severity;
    }

    public string Message { get; }
    public Severity Severity { get; }
}
