using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace ControlR.Shared.Models;

[DataContract]
public class Drive
{
    [DataMember]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DriveType DriveType { get; set; }

    [DataMember]
    public string RootDirectory { get; set; } = string.Empty;

    [DataMember]
    public string Name { get; set; } = string.Empty;

    [DataMember]
    public string DriveFormat { get; set; } = string.Empty;

    [DataMember]
    public double FreeSpace { get; set; }

    [DataMember]
    public double TotalSize { get; set; }

    [DataMember]
    public string VolumeLabel { get; set; } = string.Empty;
}
