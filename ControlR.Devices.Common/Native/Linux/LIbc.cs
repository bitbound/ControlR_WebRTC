using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ControlR.Devices.Common.Native.Linux;

public partial class Libc
{
    [LibraryImport("libc", SetLastError = true)]
    public static partial uint geteuid();
}
