using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlR.Shared.Extensions;
public static class TaskExtensions
{
    public static Task OrCompleted<T>(this T? value)
    {
        if (value is Task task)
        {
            return task;
        }
        return Task.CompletedTask;
    }
}
