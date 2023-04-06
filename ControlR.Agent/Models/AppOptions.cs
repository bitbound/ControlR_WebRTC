using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlR.Agent.Models;
internal class AppOptions
{
    public List<string> AuthorizedKeys { get; set; } = new();
    public string DeviceId { get; set; } = string.Empty;
}
