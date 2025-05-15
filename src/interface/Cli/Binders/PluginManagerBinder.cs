using System.CommandLine.Binding;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prise;
using Prise.DependencyInjection;
using TradePath.Cli.Commands;
using TradePath.Plugins.HostHelpers;
using TradePath.Plugins.HostHelpers.Prise;

namespace TradePath.Cli.Binders;

internal class PluginManagerBinder : BinderBase<IPluginManager>
{
	/// <inheritdoc />
	protected override IPluginManager GetBoundValue(BindingContext bindingContext)
	{
		using var act = Telemetry.ActivitySource.StartActivityWithParent();

		var loggerFactory = bindingContext.GetRequiredService<ILoggerFactory>();

		var pluginBinder = new OptionsBinder<PluginConfiguration>("Plugins");
		
		return new PluginManager(
			loggerFactory.CreateLogger<PluginManager>(),
			bindingContext.GetRequiredService<IMeterFactory>(),
			pluginBinder.Get(bindingContext),
			new DefaultPluginLoader(
				new PluginAssemblyScanner(
					DefaultFactories.DefaultMetadataLoadContext,
					DefaultFactories.DefaultDirectoryTraverser,
					"net9.0",
					loggerFactory.CreateLogger<PluginAssemblyScanner>()
				),
				DefaultFactories.DefaultPluginTypeSelector(),
				DefaultFactories.DefaultAssemblyLoader(),
				DefaultFactories.DefaultParameterConverter(),
				DefaultFactories.DefaultResultConverter(),
				DefaultFactories.DefaultPluginActivator()
			)
		);
	}
}
