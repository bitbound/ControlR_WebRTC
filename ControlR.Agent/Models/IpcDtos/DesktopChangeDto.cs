using ControlR.Shared.Serialization;
using MessagePack;
using System.Text.Json.Serialization;

namespace ControlR.Agent.Models.IpcDtos;

[MessagePackObject]
public class DesktopChangeDto
{
    [JsonConstructor]
    [SerializationConstructor]
    public DesktopChangeDto(string desktopName)
    {
        DesktopName = desktopName;
    }

    [MsgPackKey]
    public string DesktopName { get; set; }
}