using MessagePack;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace ControlR.Agent.Models.IpcDtos;

[DataContract]
public class DesktopChangeDto
{
    [JsonConstructor]
    [SerializationConstructor]
    public DesktopChangeDto(string desktopName)
    {
        DesktopName = desktopName;
    }

    [DataMember]
    public string DesktopName { get; set; }
}