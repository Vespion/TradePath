using System.CommandLine.Binding;
using Microsoft.Extensions.DependencyInjection;

namespace TradePath.Cli.Binders;

public class BindingContextService<T>: BinderBase<T> where T : notnull
{
	/// <inheritdoc />
	protected override T GetBoundValue(BindingContext bindingContext)
	{
		return bindingContext.GetRequiredService<T>();
	}
}
