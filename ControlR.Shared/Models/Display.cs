using System.Runtime.Serialization;

namespace ControlR.Shared.Models;

[DataContract]
public class Display
{
    [DataMember]
    public int Bottom { get; init; }

    [DataMember]
    public string DisplayId { get; init; } = string.Empty;

    public int Height => Bottom - Top;

    [DataMember]
    public bool IsPrimary { get; init; }

    [DataMember]
    public int Left { get; init; }

    [DataMember]
    public string MediaId { get; init; } = string.Empty;

    [DataMember]
    public string Name { get; init; } = string.Empty;

    [DataMember]
    public int Right { get; init; }

    [DataMember]
    public int Top { get; init; }

    public int Width => Right - Left;
}
