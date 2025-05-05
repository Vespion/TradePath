using System.CommandLine.Binding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prise;
using Prise.DependencyInjection;
using TradePath.Plugins.HostHelpers;
using TradePath.Cli.Commands;
using TradePath.Plugins.HostHelpers.Prise;

namespace TradePath.Cli.Binders;

public class PluginManagerBinder: BinderBase<IPluginManager>
{
	/// <inheritdoc />
	protected override IPluginManager GetBoundValue(BindingContext bindingContext)
	{
		var workingDirectory = bindingContext.ParseResult.GetValueForOption(GlobalOptions.WorkingDirectory)!;

		var pluginPath = Path.Combine(workingDirectory.FullName, "plugins");

		return new PluginManager(
			bindingContext.GetRequiredService<ILoggerFactory>().CreateLogger<PluginManager>(),
			new OptionsWrapper<PluginConfiguration>(new PluginConfiguration(pluginPath)),
			new DefaultPluginLoader(
				new PluginAssemblyScanner(DefaultFactories.DefaultMetadataLoadContext, DefaultFactories.DefaultDirectoryTraverser),
				DefaultFactories.DefaultPluginTypeSelector(),
				DefaultFactories.DefaultAssemblyLoader(),
				DefaultFactories.DefaultParameterConverter(),
				DefaultFactories.DefaultResultConverter(),
				DefaultFactories.DefaultPluginActivator()
			)
		);
	}
}
