using System.CommandLine.Binding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TradePath.Cli.Binders;

internal class LoggerBinder(string category) : BinderBase<ILogger>
{
	/// <inheritdoc />
	protected override ILogger GetBoundValue(BindingContext bindingContext)
	{
		using var act = Telemetry.ActivitySource.StartActivityWithParent();
		act?.AddTag("code.type.name", nameof(ILogger));
		act?.AddTag("code.type.arg", category);
		var factory = bindingContext.GetRequiredService<ILoggerFactory>();
		return factory.CreateLogger(category);
	}
}
