using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using ControlR.Agent.Services;
using ControlR.Agent.Interfaces;
using ControlR.Devices.Common.Services.Interfaces;
using ControlR.Devices.Common.Services.Windows;
using ControlR.Devices.Common.Services.Linux;
using ControlR.Devices.Common.Services;
using System.CommandLine.Parsing;
using System.CommandLine.NamingConventionBinder;
using ControlR.Shared.Services;
using Microsoft.Extensions.Configuration;
using ControlR.Agent.Services.Windows;
using ControlR.Agent.Services.Linux;
using ControlR.Shared.Services.Http;
using System.Reflection;
using ControlR.Agent.Models;
using EasyIpc;

var rootCommand = new RootCommand("Provides zero-trust remote control and remote administration.");

var installCommand = new Command("install", "Install the ControlR service.");

var unInstallCommand = new Command("uninstall", "Uninstall the ControlR service.");

var watchDesktopCommand = new Command("watch-desktop", "Watches for desktop changes (winlogon/UAC) for the streamer process.");
var agentPipeOption = new Option<string>(
    new[] { "-a", "--agent-pipe" },
    "The agent pipe name to which the watcher should connect.")
{
    IsRequired = true
};
var parentIdOption = new Option<int>(
    new[] { "-p", "--parent-id" },
    "The calling process's ID.")
{
    IsRequired = true
};

var runCommand = new Command("run", "Run the ControlR service.");
var authorizedKeyOption = new Option<string>(
    new[] { "-a", "--authorized-key" },
    "An optional public key to preconfigure with authorization to this device.");


installCommand.AddOption(authorizedKeyOption);
watchDesktopCommand.AddOption(agentPipeOption);
watchDesktopCommand.AddOption(parentIdOption);
rootCommand.AddCommand(installCommand);
rootCommand.AddCommand(runCommand);
rootCommand.AddCommand(unInstallCommand);
rootCommand.AddCommand(watchDesktopCommand);

installCommand.SetHandler(async (authorizedKey) =>
{
    var host = CreateHost(StartupMode.Install);
    var installer = host.Services.GetRequiredService<IAgentInstaller>();
    await installer.Install(authorizedKey);
    await host.RunAsync();
}, authorizedKeyOption);

runCommand.SetHandler(async () =>
{
    var host = CreateHost(StartupMode.Run);

    var appDir = EnvironmentHelper.Instance.StartupDirectory;
    var appSettingsPath = Path.Combine(appDir!, "appsettings.json");

    if (!File.Exists(appSettingsPath))
    {
        using var mrs = Assembly.GetExecutingAssembly().GetManifestResourceStream("ControlR.Agent.appsettings.json");
        using var fs = new FileStream(appSettingsPath, FileMode.Create);
        await mrs!.CopyToAsync(fs);
    }

    var hubConnection = host.Services.GetRequiredService<IAgentHubConnection>();
    await hubConnection.Start();
    await host.RunAsync();
});

unInstallCommand.SetHandler(async () =>
{
    var host = CreateHost(StartupMode.Uninstall);
    var installer = host.Services.GetRequiredService<IAgentInstaller>();
    await installer.Uninstall();
    await host.RunAsync();
});

watchDesktopCommand.SetHandler(async (agentPipeName, parentProcessId) =>
{
    var host = CreateHost(StartupMode.WatchDesktop);
    var desktopReporter = host.Services.GetRequiredService<IInputDesktopReporter>();
    await desktopReporter.Start(agentPipeName, parentProcessId);
    await host.RunAsync();
}, agentPipeOption, parentIdOption);

return await rootCommand.InvokeAsync(args);


IHost CreateHost(StartupMode startupMode)
{
    var host = Host.CreateDefaultBuilder(args);

    if (Environment.UserInteractive)
    {
        host.UseConsoleLifetime();
    }
    else if (OperatingSystem.IsWindows())
    {
        host.UseWindowsService(config =>
        {
            config.ServiceName = "ControlR.Agent";
        });
    }
    else if (OperatingSystem.IsLinux())
    {
        host.UseSystemd();
    }

    host.ConfigureAppConfiguration((context, config) =>
    {
        var appDir = EnvironmentHelper.Instance.StartupDirectory;
        var appSettingsPath = Path.Combine(appDir!, "appsettings.json");

        config
            .AddEnvironmentVariables()
            .AddJsonFile(Path.Combine(appSettingsPath), true, true)
            .AddJsonFile(Path.Combine(appDir!, $"appsettings.{context.HostingEnvironment.EnvironmentName}.json"), true, true);
    });

    host.ConfigureServices((context, services) =>
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

        if (startupMode == StartupMode.WatchDesktop && OperatingSystem.IsWindows())
        {
            services.AddSingleton<IInputDesktopReporter, InputDesktopReporter>();
        }

        services.AddSingleton<IProcessInvoker, ProcessInvoker>();
        services.AddSingleton<IEnvironmentHelper>(_ => EnvironmentHelper.Instance);
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IProcessInvoker, ProcessInvoker>();
        services.AddTransient<IHubConnectionBuilder, HubConnectionBuilder>();
        services.AddSingleton<IEncryptionSessionFactory, EncryptionSessionFactory>();
        services.AddEasyIpc();

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

    return host.Build();
}