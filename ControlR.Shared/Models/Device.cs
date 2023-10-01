using ControlR.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace ControlR.Shared.Models;

[DataContract]
public class Device
{
    [DataMember]
    [Display(Name = "Agent Version")]
    public string AgentVersion { get; set; } = string.Empty;

    [DataMember]
    [StringLength(100)]
    [Display(Name = "Alias")]
    public string Alias { get; set; } = string.Empty;

    [DataMember]
    [Display(Name = "Authorized Keys")]
    public IEnumerable<string> AuthorizedKeys { get; set; } = Array.Empty<string>();

    [DataMember]
    [Display(Name = "CPU Utilization")]
    public double CpuUtilization { get; set; }

    [DataMember]
    [Display(Name = "Current User")]
    public string CurrentUser { get; set; } = string.Empty;

    [DataMember]
    [Display(Name = "Drives")]
    public List<Drive> Drives { get; set; } = new();


    [DataMember]
    [Display(Name = "Device Id")]
    public string Id { get; set; } = string.Empty;

    [DataMember]
    [Display(Name = "64-bit")]
    public bool Is64Bit { get; set; }

    [DataMember]
    [Display(Name = "Online")]
    public bool IsOnline { get; set; }

    [DataMember]
    [Display(Name = "Last Online")]
    public DateTimeOffset LastOnline { get; set; }

    [DataMember]
    [Display(Name = "Device Name")]
    public string Name { get; set; } = string.Empty;

    [DataMember]
    [StringLength(5000)]
    [Display(Name = "Notes")]
    public string Notes { get; set; } = string.Empty;

    [DataMember]
    [Display(Name = "OS Architecture")]
    public Architecture OsArchitecture { get; set; }
    [DataMember]
    [Display(Name = "OS Description")]
    public string OsDescription { get; set; } = string.Empty;

    [DataMember]
    [Display(Name = "Platform")]
    public SystemPlatform Platform { get; set; }

    [DataMember]
    [Display(Name = "Processor Count")]
    public int ProcessorCount { get; set; }

    [DataMember]
    [Display(Name = "Public IP")]
    public string PublicIP { get; set; } = string.Empty;

    [DataMember]
    [StringLength(200)]
    [Display(Name = "Tags")]
    public string Tags { get; set; } = string.Empty;

    [DataMember]
    [Display(Name = "Memory Total")]
    public double TotalMemory { get; set; }

    [DataMember]
    [Display(Name = "Storage Total")]
    public double TotalStorage { get; set; }

    [DataMember]
    [Display(Name = "Memory Used")]
    public double UsedMemory { get; set; }

    [IgnoreDataMember]
    [JsonIgnore]
    [Display(Name = "Memory Used %")]
    public double UsedMemoryPercent => UsedMemory / TotalMemory;

    [DataMember]
    [Display(Name = "Storage Used")]
    public double UsedStorage { get; set; }

    [IgnoreDataMember]
    [JsonIgnore]
    [Display(Name = "Storage Used %")]
    public double UsedStoragePercent => UsedStorage / TotalStorage;
}
