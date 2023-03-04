using ControlR.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlR.Shared.Services;

public interface IEnvironmentHelper
{
    bool IsDebug { get; }
    bool IsWindows { get; }
    Platform Platform { get; }
    string StartupDirectory { get; }
    string StartupExePath { get; }
}
internal class EnvironmentHelper : IEnvironmentHelper
{
    public static EnvironmentHelper Instance { get; } = new();
    public bool IsDebug
    {
        get
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }
    }

    public bool IsWindows => OperatingSystem.IsWindows();

    public Platform Platform
    {
        get
        {
            if (OperatingSystem.IsWindows())
            {
                return Platform.Windows;
            }
            else if (OperatingSystem.IsLinux())
            {
                return Platform.Linux;
            }
            else if (OperatingSystem.IsMacOS())
            {
                return Platform.MacOS;
            }
            else if (OperatingSystem.IsMacCatalyst())
            {
                return Platform.MacCatalyst;
            }
            else if (OperatingSystem.IsAndroid())
            {
                return Platform.Android;
            }
            else if (OperatingSystem.IsIOS())
            {
                return Platform.IOS;
            }
            else if (OperatingSystem.IsBrowser())
            {
                return Platform.Browser;
            }
            else
            {
                return Platform.Unknown;
            }
        }
    }

    public string StartupDirectory => Path.GetDirectoryName(StartupExePath) ?? AppContext.BaseDirectory;

    public string StartupExePath { get; } = Environment.ProcessPath ?? Environment.GetCommandLineArgs().First();
}
