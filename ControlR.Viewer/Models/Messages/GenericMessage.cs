namespace ControlR.Viewer.Models.Messages;

public class GenericMessage<T>
    where T : notnull
{
    public GenericMessage(T value)
    {
        Value = value;
    }

    public T Value { get; }
}
