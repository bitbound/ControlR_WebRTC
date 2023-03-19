using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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