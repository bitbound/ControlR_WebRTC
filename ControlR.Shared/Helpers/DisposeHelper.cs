using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlR.Shared.Helpers;
public static class DisposeHelper
{
    public static void DisposeAll(params IDisposable?[] disposables)
    {
        foreach (var disposable in disposables)
        {
            try
            {
                disposable?.Dispose();
            }
            catch { }
        }
    }
}
