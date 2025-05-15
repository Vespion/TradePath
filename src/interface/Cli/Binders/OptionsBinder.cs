using System.CommandLine;
using System.CommandLine.Binding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace TradePath.Cli.Binders;

public class OptionsBinder<T>(string sectionPath = ""): BinderBase<IOptions<T>> where T : class
{
	/// <inheritdoc />
	protected override IOptions<T> GetBoundValue(BindingContext bindingContext)
	{
		var config = bindingContext.GetRequiredService<IConfiguration>();

		var section = config.GetSection(sectionPath);

		var optionsConfigurator = new NamedConfigureFromConfigurationOptions<T>(Options.DefaultName, section);
		
		var optionFactory = new OptionsFactory<T>([optionsConfigurator], []);

		var optionsManager = new OptionsManager<T>(optionFactory);

		return optionsManager;
	}
	
	public IOptions<T> Get(BindingContext bindingContext)
	{
		return GetBoundValue(bindingContext);
	}
}
