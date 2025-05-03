using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using Prise;
using Prise.DependencyInjection;
using Spectre.Console;
using TradePath.Plugins.HostHelpers;
using TradePath.Plugins.HostHelpers.Progress;
using TradePath.Cli.Binders;
using TradePath.Plugins.Contracts;
using PluginName = TradePath.Plugins.HostHelpers.PluginName;

namespace TradePath.Cli.Commands;

public class PluginCommands
{
	public static Command Build()
	{
		var cmd = new Command(Resources.Commands.Plugin_Name, Resources.Commands.Plugin_Description)
		{
			BuildInstallCommand(),
			BuildUninstallCommand(),
			BuildListCommand(),
			BuildSearchCommand()
		};
		cmd.AddGlobalOption(GlobalOptions.WorkingDirectory);
		return cmd;
	}
	
	private static Command BuildListCommand()
	{
		var cmd = new Command(Resources.Commands.Plugin_List_Name, Resources.Commands.Plugin_List_Description);

		// cmd.SetHandler(Handle,
		// 	new LoggerBinder("TradePath.Cli.Commands.Plugin.List"),
		// 	GlobalOptions.Verbosity,
		// 	new BindingContextService<IAnsiConsole>(),
		// 	new PluginInstallationBinder()
		// );
		return cmd;

		// async Task<int> Handle(ILogger logger, LogLevel verbosity, IAnsiConsole console, PluginInstallationContext pluginInstallation)
		// {
		// 	logger.LogDebug("Querying nuget project");
		//
		// 	IReadOnlyCollection<PluginSpec> installedPlugins = null!;
		// 	if (verbosity >= LogLevel.Information)
		// 	{
		// 		await console.Progress()
		// 			.StartAsync(async ctx =>
		// 			{
		// 				var progress = new Progress<GetInstalledPluginsProgress>();
		//
		// 				var packageTask = ctx.AddTask("");
		//
		// 				progress.ProgressChanged += OnProgressOnProgressChanged;
		//
		// 				installedPlugins = await pluginInstallation.GetInstalledPlugins(progress);
		//
		// 				packageTask.StopTask();
		// 				progress.ProgressChanged -= OnProgressOnProgressChanged;
		// 				
		// 				return;
		//
		// 				void OnProgressOnProgressChanged(object? _, GetInstalledPluginsProgress args)
		// 				{
		// 					packageTask.Description = args.CurrentStage switch
		// 					{
		// 						GetInstalledPluginsProgress.Stage.ScanningInstallation => Resources.Commands.Plugin_List_Messages_ScanningInstallation,
		// 						GetInstalledPluginsProgress.Stage.ReadingPackageManifests => Resources.Commands.Plugin_List_Messages_ReadingManifests,
		// 						_ => ""
		// 					};
		//
		// 					packageTask.Value = args.Step ?? 0;
		// 					packageTask.MaxValue = args.TotalSteps ?? 0;
		// 					packageTask.IsIndeterminate = args.Step == null || args.TotalSteps == null;
		// 				}
		// 			});
		// 	}
		// 	else
		// 	{
		// 		installedPlugins = await pluginInstallation.GetInstalledPlugins();
		// 	}
		// 	
		// 	logger.LogDebug("Plugin installation scan complete");
		// 	
		// 	if (installedPlugins.Count == 0)
		// 	{
		// 		console.MarkupLine($"[red]{Resources.Commands.Plugin_Messages_NoPluginsInstalled}[/]");
		// 		return 1;
		// 	}
		//
		// 	var table = new Table();
		// 		
		// 	table.AddColumn("[green]Name[/]");
		// 	table.AddColumn("[green]Authors[/]");
		// 	table.AddColumn("[green]License[/]");
		// 	table.AddColumn("[green]Supported Features[/]");
		// 		
		// 	foreach (var plugin in installedPlugins)
		// 	{
		// 		var features = new List<string>(1);
		//
		// 		if (plugin.SupportsSystemNavigationProvider)
		// 		{
		// 			features.Add(TradePath.Plugins.HostHelpers.Resources.Plugins.Plugin_Feature_SystemNavigationProvider);
		// 		}
		// 			
		// 		table.AddRow(
		// 			plugin.Name,
		// 			string.Join(", ", plugin.Authors),
		// 			plugin.License ?? "Ø",
		// 			string.Join(", ", features)
		// 		);
		// 	}
		//
		// 	return 0;
		// }
	}
	
	private static Command BuildSearchCommand()
	{
		var cmd = new Command(Resources.Commands.Plugin_Search_Name, Resources.Commands.Plugin_Search_Description);
		return cmd;
	}
	
	private static Command BuildInstallCommand()
	{
		var cmd = new Command(Resources.Commands.Plugin_Install_Name, Resources.Commands.Plugin_Install_Description);
		cmd.SetHandler(Handle,
			new LoggerBinder("TradePath.Cli.Commands.Plugin.Install"),
			GlobalOptions.Verbosity,
			new BindingContextService<IAnsiConsole>(),
			new PluginManagerBinder()
		);
		return cmd;

		async Task<int> Handle(ILogger logger, LogLevel verbosity, IAnsiConsole console,
			IPluginManager pluginInstallation)
		{
			await pluginInstallation.InstallPlugin(new PluginInstallConfiguration(
				PluginName.From("TradePath.Plugins.Edsm"),
				VersionRange.All,
				false
			));

			return 1;

			var sc = new ServiceCollection();

			sc.AddPrise();
			sc.AddLogging(lb => lb.AddConsole());
			
			var sp = sc.BuildServiceProvider();
			
			var loader = sp.GetRequiredService<IPluginLoader>();

			var pluginDir = Path.GetFullPath("./plugins");
			
			var scan = await loader.FindPlugin<ISystemNavigationProvider>(pluginDir);
			var plugin = await loader.LoadPlugin<ISystemNavigationProvider>(scan);

			try
			{
				plugin.GetStationData(null);
			}
			catch (NotImplementedException)
			{
				
			}
			
			return 0;
		}
	}
	
	private static Command BuildUninstallCommand()
	{
		var cmd = new Command(Resources.Commands.Plugin_Uninstall_Name, Resources.Commands.Plugin_Uninstall_Description);
		return cmd;
	}
}
