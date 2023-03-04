using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlR.Viewer.Services.Logging;
internal class FileLoggerProvider : ILoggerProvider
{

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(categoryName);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}