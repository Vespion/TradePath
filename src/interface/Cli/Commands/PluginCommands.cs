using System.Collections.ObjectModel;
using System.CommandLine;
using System.CommandLine.Parsing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using Prise;
using Prise.DependencyInjection;
using Spectre.Console;
using TradePath.Plugins.HostHelpers;
using TradePath.Cli.Binders;
using TradePath.Plugins.Contracts;
using TradePath.Plugins.HostHelpers.Models;

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
		
		var pluginIdArgument = new Argument<string>(
			Resources.Commands.Plugin_Install_Arguments_Id_Name,
			Resources.Commands.Plugin_Install_Arguments_Id_Description
		)
		{
			Arity = ArgumentArity.ExactlyOne
		};
		
		var pluginVersionOption = new Option<VersionRange?>(
			Resources.Commands.Plugin_Install_Options_Version_Name.Split('|'),
			parseArgument: result =>
			{
				var str = result.Tokens.Single().Value;
				
				if (VersionRange.TryParse(str, out var versionRange))
				{
					return versionRange;
				}

				result.ErrorMessage = string.Format(Resources.Commands.Plugin_Install_Messages_InvalidVersionRange, str);

				return null;
			},
			false,
			Resources.Commands.Plugin_Install_Options_Version_Description
		)
		{
			IsRequired = false,
			Arity = ArgumentArity.ExactlyOne
		};
		
		var pluginPreleaseOption = new Option<bool>(
			Resources.Commands.Plugin_Install_Options_PreRelease_Name.Split('|'),
			Resources.Commands.Plugin_Install_Options_PreRelease_Description
		)
		{
			IsRequired = false,
			Arity = ArgumentArity.ZeroOrOne
		};
		
		cmd.AddArgument(pluginIdArgument);
		cmd.AddOption(pluginVersionOption);
		cmd.AddOption(pluginPreleaseOption);
		cmd.SetHandler(HandlePluginInstall,
			new LoggerBinder("TradePath.Cli.Commands.Plugin.Install"),
			GlobalOptions.Verbosity,
			new BindingContextService<IAnsiConsole>(),
			new PluginManagerBinder(),
			pluginIdArgument,
			pluginVersionOption,
			pluginPreleaseOption
		);
		return cmd;
	}

	private static async Task<int> HandlePluginInstall(ILogger logger, LogLevel verbosity, IAnsiConsole console, IPluginManager pluginInstallation, string pluginId, VersionRange? pluginVersion, bool allowPreRelease)
	{
		var installConfig = new PluginInstallConfiguration(
			PluginName.From(pluginId),
			pluginVersion,
			allowPreRelease
		);

		if (!console.Profile.Out.IsTerminal)
		{
			await pluginInstallation.InstallPluginAsync(installConfig);
		}
		else
		{
			await HandlePluginInstallWithConsole(console, pluginInstallation, installConfig);
		}
			
		return 0;
	}

	private static async Task HandlePluginInstallWithConsole(IAnsiConsole console, IPluginManager pluginInstallation, PluginInstallConfiguration installConfig)
	{
		await console
			.Progress()
			.StartAsync(async ctx =>
			{
				var rootResolutionTask = ctx.AddTask(
					Resources.Commands.Plugin_Install_Messages_ResolvingDependencies,
					true, 3
				);
				ProgressTask? rootInstallationTask = null;
					
				var resolutionTasks = new Dictionary<string, ProgressTask>();
				var installationTasks = new Dictionary<string, ProgressTask>();
					
				var progress = new Progress<PluginInstallationProgress>();
				
				progress.ProgressChanged += OnProgressChanged;

				await pluginInstallation.InstallPluginAsync(installConfig, progress);
					
				progress.ProgressChanged -= OnProgressChanged;
				return;

				void HandleNewResolutionUpdates(IDictionary<string, string>? newResolutionTasks)
				{
					foreach (var newResolution in newResolutionTasks ?? ReadOnlyDictionary<string, string>.Empty)
					{
						if (!resolutionTasks.TryGetValue(newResolution.Key, out var parentTask))
						{
							parentTask = rootResolutionTask;
						}
						resolutionTasks[newResolution.Value] = ctx.AddTaskAfter(
							newResolution.Value,
							parentTask
						).IsIndeterminate();
					}
				}
				
				void HandleResolutionCompletions(ICollection<string>? completedResolutionTasks)
				{
					foreach (var completedResolution in completedResolutionTasks ?? Array.Empty<string>())
					{
						if (resolutionTasks.TryGetValue(completedResolution, out var task))
						{
							task.StopTask();
						}
					}
				}

				void HandleInstallationTasks(IDictionary<string, int> tasks)
				{
					rootResolutionTask.StopTask();
				
					rootInstallationTask ??= ctx.AddTask(
						Resources.Commands.Plugin_Install_Messages_InstallingPlugin,
						true, tasks.Count
					);
							
					foreach (var (name, p) in tasks)
					{
						if (!installationTasks.TryGetValue(name, out var task))
						{
							task = ctx.AddTaskAfter(
								name,
								rootInstallationTask,
								false,
								4
							);
							installationTasks[name] = task;
						}
						
						UpdateTask(p, task);
					}

					void UpdateTask(int p, ProgressTask task)
					{
						task.Value = p;
						switch (p)
						{
							case > 0 when !task.IsStarted:
								task.StartTask();
								break;
							case >= 4:
								task.StopTask();
								break;
						}
					}
				}
				
				void OnProgressChanged(object? _, PluginInstallationProgress e)
				{
					HandleNewResolutionUpdates(e.NewResolutionTasks);
					HandleResolutionCompletions(e.CompletedResolutionTasks);
						
					if (e.RunningSimplification)
					{
						rootResolutionTask
							.Value(1)
							.Description = Resources.Commands.Plugin_Install_Messages_SimplifyingDependencies;
					}
						
					if (e.InstallationTasks != null)
					{
						HandleInstallationTasks(e.InstallationTasks);
					}
				}
			});
	}

	private static Command BuildUninstallCommand()
	{
		var cmd = new Command(Resources.Commands.Plugin_Uninstall_Name, Resources.Commands.Plugin_Uninstall_Description);
		return cmd;
	}
}
