using System.CommandLine.Binding;
using Microsoft.Extensions.DependencyInjection;

namespace TradePath.Cli.Binders;

internal class BindingContextService<T> : BinderBase<T> where T : notnull
{
	/// <inheritdoc />
	protected override T GetBoundValue(BindingContext bindingContext)
	{
		using var act = Telemetry.ActivitySource.StartActivityWithParent();
		act?.AddTag("code.type.name", nameof(T));
		return bindingContext.GetRequiredService<T>();
	}
}
