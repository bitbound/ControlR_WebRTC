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
