using System.Runtime.Serialization;

namespace ControlR.Shared.Models;

[DataContract]
public class Display
{

    [DataMember]
    public string DisplayId { get; init; } = string.Empty;

    [DataMember]
    public int Height { get; init; }

    [DataMember]
    public bool IsPrimary { get; init; }

    [DataMember]
    public int Left { get; init; }

    [DataMember]
    public string MediaId { get; init; } = string.Empty;

    [DataMember]
    public string Name { get; init; } = string.Empty;

    [DataMember]
    public int Top { get; init; }

    [DataMember]
    public int Width { get; init; }
}
