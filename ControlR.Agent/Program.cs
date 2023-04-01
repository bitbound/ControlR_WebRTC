using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.CommandLine;
using ControlR.Agent.Services;
using ControlR.Agent.Interfaces;
using System.CommandLine.Parsing;
using ControlR.Shared.Services;
using ControlR.Agent.Services.Windows;
using System.Reflection;
using ControlR.Agent.Models;
using ControlR.Agent.Configuration;

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
    if (!OperatingSystem.IsWindows())
    {
        Console.WriteLine("This command is only available on Windows.");
        return;
    }
    var host = CreateHost(StartupMode.WatchDesktop);
    var desktopReporter = host.Services.GetRequiredService<IInputDesktopReporter>();
    await desktopReporter.Start(agentPipeName, parentProcessId);
    await host.RunAsync();
}, agentPipeOption, parentIdOption);

return await rootCommand.InvokeAsync(args);


IHost CreateHost(StartupMode startupMode)
{
    var host = Host.CreateDefaultBuilder(args);
    host.AddControlRAgent(startupMode);
    return host.Build();
}