using MudBlazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
