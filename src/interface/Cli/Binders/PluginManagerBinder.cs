using System.CommandLine.Binding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TradePath.Plugins.HostHelpers;
using TradePath.Cli.Commands;

namespace TradePath.Cli.Binders;

public class PluginManagerBinder: BinderBase<IPluginManager>
{
	/// <inheritdoc />
	protected override IPluginManager GetBoundValue(BindingContext bindingContext)
	{
		var workingDirectory = bindingContext.ParseResult.GetValueForOption(GlobalOptions.WorkingDirectory)!;

		var pluginPath = Path.Combine(workingDirectory.FullName, "plugins");

		return new PluginManager(pluginPath, bindingContext.GetRequiredService<ILoggerFactory>().CreateLogger<PluginManager>());
	}
}
