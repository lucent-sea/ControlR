﻿using ControlR.Agent.Interfaces;
using ControlR.Agent.Models;
using ControlR.Agent.Services;
using ControlR.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.CommandLine;
using System.Reflection;

namespace ControlR.Agent.Startup;

internal class CommandProvider
{
    private static readonly string[] _authorizedKeyAlias = ["-a", "--authorized-key"];
    private static readonly string[] _autoRunVnc = ["-r", "--auto-run"];
    private static readonly string[] _portAlias = ["-p", "--port"];

    internal static Command GetInstallCommand(string[] args)
    {
        var authorizedKeyOption = new Option<string>(
            _authorizedKeyAlias,
            "An optional public key to preconfigure with authorization to this device.");

        var portOption = new Option<int?>(
            _portAlias,
            "The port to use for VNC connections.  ControlR will proxy viewer connections to this port.");

        var autoRunOption = new Option<bool?>(
             _autoRunVnc,
             "Whether to automatically download (if needed) and run a temporary TightVNC server. " +
             "The server will run in loopback-only mode, and a new random password will be generated " +
             "for each session. The server will shutdown when the session ends. Set this to false " +
             "to use an existing server.");

        var installCommand = new Command("install", "Install the ControlR service.")
        {
            authorizedKeyOption,
            portOption,
            autoRunOption
        };

        installCommand.SetHandler(async (authorizedKey, vncPort, auotInstallVnc) =>
        {
            var host = CreateHost(StartupMode.Install, args);
            var installer = host.Services.GetRequiredService<IAgentInstaller>();
            await installer.Install(authorizedKey, vncPort, auotInstallVnc);
            await host.RunAsync();
        }, authorizedKeyOption, portOption, autoRunOption);

        return installCommand;
    }

    internal static Command GetRunCommand(string[] args)
    {
        var runCommand = new Command("run", "Run the ControlR service.");

        runCommand.SetHandler(async () =>
        {
            var host = CreateHost(StartupMode.Run, args);

            var appDir = EnvironmentHelper.Instance.StartupDirectory;
            var appSettingsPath = Path.Combine(appDir!, "appsettings.json");

            if (!File.Exists(appSettingsPath))
            {
                using var mrs = Assembly.GetExecutingAssembly().GetManifestResourceStream("ControlR.Agent.appsettings.json");
                if (mrs is not null)
                {
                    using var fs = new FileStream(appSettingsPath, FileMode.Create);
                    await mrs.CopyToAsync(fs);
                }
            }

            var hubConnection = host.Services.GetRequiredService<IAgentHubConnection>();
            await host.RunAsync();
        });

        return runCommand;
    }

    internal static Command GetUninstallCommand(string[] args)
    {
        var unInstallCommand = new Command("uninstall", "Uninstall the ControlR service.");
        unInstallCommand.SetHandler(async () =>
        {
            var host = CreateHost(StartupMode.Uninstall, args);
            var installer = host.Services.GetRequiredService<IAgentInstaller>();
            await installer.Uninstall();
            await host.RunAsync();
        });
        return unInstallCommand;
    }

    private static IHost CreateHost(StartupMode startupMode, string[] args)
    {
        var host = Host.CreateDefaultBuilder(args);
        host.AddControlRAgent(startupMode);
        return host.Build();
    }
}