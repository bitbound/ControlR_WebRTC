﻿using ControlR.Agent.Interfaces;
using ControlR.Agent.Models;
using ControlR.Agent.Services;
using ControlR.Agent.Services.Linux;
using ControlR.Agent.Services.Windows;
using ControlR.Devices.Common.Services;
using ControlR.Devices.Common.Services.Interfaces;
using ControlR.Devices.Common.Services.Linux;
using ControlR.Devices.Common.Services.Windows;
using ControlR.Shared.Services;
using ControlR.Shared.Services.Http;
using SimpleIpc;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ControlR.Agent.Startup;
internal static class IHostBuilderExtensions
{
    internal static IHostBuilder AddControlRAgent(this IHostBuilder builder, StartupMode startupMode)
    {
        if (Environment.UserInteractive)
        {
            builder.UseConsoleLifetime();
        }
        else if (OperatingSystem.IsWindows())
        {
            builder.UseWindowsService(config =>
            {
                config.ServiceName = "ControlR.Agent";
            });
        }
        else if (OperatingSystem.IsLinux())
        {
            builder.UseSystemd();
        }

        builder.ConfigureAppConfiguration((context, config) =>
        {
            var appDir = EnvironmentHelper.Instance.StartupDirectory;
            var appSettingsPath = Path.Combine(appDir!, "appsettings.json");

            config
                .AddEnvironmentVariables()
                .AddJsonFile(Path.Combine(appSettingsPath), true, true)
                .AddJsonFile(Path.Combine(appDir!, $"appsettings.{context.HostingEnvironment.EnvironmentName}.json"), true, true);
        });

        builder.ConfigureServices((context, services) =>
        {
            services.Configure<AppOptions>(
                context.Configuration.GetSection(nameof(AppOptions)));

            services.AddHttpClient<IDownloadsApi, DownloadsApi>();


            if (startupMode == StartupMode.Run)
            {
                if (OperatingSystem.IsWindows())
                {
                    services.AddHostedService<StreamingSessionWatcher>();
                }

                services.AddSingleton<IAgentUpdater, AgentUpdater>();
                services.AddHostedService(services => services.GetRequiredService<IAgentUpdater>());
                services.AddHostedService<AgentHeartbeatTimer>();
                services.AddHostedService<DtoHandler>();
                services.AddSingleton<ICpuUtilizationSampler, CpuUtilizationSampler>();
                services.AddHostedService(services => services.GetRequiredService<ICpuUtilizationSampler>());
                services.AddSingleton<IAgentHubConnection, AgentHubConnection>();
            }

            if (startupMode == StartupMode.Sidecar && OperatingSystem.IsWindows())
            {
                services.AddSingleton<IInputDesktopReporter, InputDesktopReporter>();
            }

            services.AddSingleton<IProcessInvoker, ProcessInvoker>();
            services.AddSingleton<IEnvironmentHelper>(_ => EnvironmentHelper.Instance);
            services.AddSingleton<IFileSystem, FileSystem>();
            services.AddSingleton<IProcessInvoker, ProcessInvoker>();
            services.AddTransient<IHubConnectionBuilder, HubConnectionBuilder>();
            services.AddSingleton<IEncryptionSessionFactory, EncryptionSessionFactory>();
            services.AddSimpleIpc();

            if (OperatingSystem.IsWindows())
            {
                services.AddSingleton<IStreamingSessionCache, StreamingSessionCache>();
                services.AddSingleton<IDeviceDataGenerator, DeviceDataGeneratorWin>();
                services.AddSingleton<IAgentInstaller, AgentInstallerWindows>();
                services.AddSingleton<IRemoteControlLauncher, RemoteControlLauncherWindows>();
                services.AddSingleton<IPowerControl, PowerControlWindows>();
            }
            else if (OperatingSystem.IsLinux())
            {
                services.AddSingleton<IDeviceDataGenerator, DeviceDataGeneratorLinux>();
                services.AddSingleton<IAgentInstaller, AgentInstallerLinux>();
                services.AddSingleton<IRemoteControlLauncher, RemoteControlLauncherLinux>();
                services.AddSingleton<IPowerControl, PowerControlLinux>();
            }
            else
            {
                throw new PlatformNotSupportedException("Only Windows and Linux are supported.");
            }
        })
        .ConfigureLogging(logging =>
        {
            logging.AddConsole();
            logging.AddDebug();
            var version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.0.0";
            logging.AddProvider(new FileLoggerProvider("ControlR.Agent", version));
            logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning);
        });

        return builder;
    }
}
