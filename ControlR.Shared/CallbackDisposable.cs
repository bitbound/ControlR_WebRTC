using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlR.Shared;

public sealed class CallbackDisposable : IDisposable
{
    private readonly Action _disposeCallback;

    public CallbackDisposable(Action disposeCallback)
    {
        _disposeCallback = disposeCallback;
    }

    public void Dispose()
    {
        try
        {
            _disposeCallback();
        }
        catch { }
    }
}
