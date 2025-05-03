using System.CommandLine.Binding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TradePath.Cli.Binders;

public class LoggerBinder(string category): BinderBase<ILogger>
{
	/// <inheritdoc />
	protected override ILogger GetBoundValue(BindingContext bindingContext)
	{
		var factory = bindingContext.GetRequiredService<ILoggerFactory>();
		return factory.CreateLogger(category);
	}
}
