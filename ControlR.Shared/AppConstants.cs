using ControlR.Shared.Enums;
using ControlR.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ControlR.Shared;

public static partial class AppConstants
{
    public const string AgentCertificateThumbprint = "4b6235f1c44ab3a5f29bf40ad85b442269f6ee52";

    public static string AgentFileName
    {
        get
        {
            return EnvironmentHelper.Instance.Platform switch
            {
                Platform.Windows => "ControlR.Agent.exe",
                Platform.Linux => "ControlR.Agent",
                Platform.MacOS => throw new PlatformNotSupportedException(),
                Platform.MacCatalyst => throw new PlatformNotSupportedException(),
                _ => throw new PlatformNotSupportedException(),
            };
        }
    }

    public static string RemoteControlFileName
    {
        get
        {
            return EnvironmentHelper.Instance.Platform switch
            {
                Platform.Windows => "controlr-streamer.exe",
                Platform.Linux => "controlr-streamer",
                Platform.MacOS => throw new PlatformNotSupportedException(),
                Platform.MacCatalyst => throw new PlatformNotSupportedException(),
                _ => throw new PlatformNotSupportedException(),
            };
        }
    }

    public static string RemoteControlZipFileName
    {
        get
        {
            return EnvironmentHelper.Instance.Platform switch
            {
                Platform.Windows => "controlr-streamer-win.zip",
                Platform.Linux => "controlr-streamer-linux.zip",
                Platform.MacOS => throw new PlatformNotSupportedException(),
                Platform.MacCatalyst => throw new PlatformNotSupportedException(),
                _ => throw new PlatformNotSupportedException(),
            };
        }
    }

    public static string ServerUri
    {
        get
        {
            var envUri = Environment.GetEnvironmentVariable("ControlRServerUri");
            if (!string.IsNullOrWhiteSpace(envUri))
            {
                return envUri;
            }

            if (EnvironmentHelper.Instance.IsDebug)
            {
                return "https://localhost:7031";
            }
            //return "https://controlr.app";
            return "http://192.168.0.2:5007";
        }
    }

    public static string GetDesktopWatcherMmfName(int streamerProcessId, int watcherProcessId)
    {
        return $"ControlR-Desktop-Watcher-{streamerProcessId}-{watcherProcessId}";
    }


    [GeneratedRegex("[^A-Za-z0-9_-]")]
    public static partial Regex UsernameValidator();
}
