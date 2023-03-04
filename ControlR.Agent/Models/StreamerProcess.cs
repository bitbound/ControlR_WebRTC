using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlR.Agent.Models;
internal class StreamerProcess
{
    public StreamerProcess(int processId)
    {
        ProcessId = processId;
    }

    public int ProcessId { get; }
}
