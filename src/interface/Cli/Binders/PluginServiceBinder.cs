using System.CommandLine.Binding;
using Microsoft.Extensions.DependencyInjection;
using Prise;
using Prise.DependencyInjection;

namespace TradePath.Cli.Binders;

public class PluginServiceBinder: BinderBase<IPluginLoader>
{
	/// <inheritdoc />
	protected override IPluginLoader GetBoundValue(BindingContext bindingContext)
	{
		var sc = new ServiceCollection();
		sc.AddPrise();

		var sp = sc.BuildServiceProvider();
		
		return sp.GetRequiredService<IPluginLoader>();
	}
}
