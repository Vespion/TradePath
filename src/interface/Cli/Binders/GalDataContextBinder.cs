using System.CommandLine.Binding;
using Microsoft.EntityFrameworkCore;
using TradePath.Cli.Commands;
using TradePath.DataStore.Database;

namespace TradePath.Cli.Binders;

internal class GalDataContextBinder : BinderBase<GalDataContext>
{
	/// <inheritdoc />
	protected override GalDataContext GetBoundValue(BindingContext bindingContext)
	{
		using var act = Telemetry.ActivitySource.StartActivityWithParent();
		act?.AddTag("code.type.name", nameof(GalDataContext));
		var workingDirectory = bindingContext.ParseResult.GetValueForOption(GlobalOptions.WorkingDirectory)!;

		var dbPath = Path.Combine(workingDirectory.FullName, "gal.db");

		var optionsBuilder = new DbContextOptionsBuilder<GalDataContext>();
		// optionsBuilder.EnableDetailedErrors();
		// optionsBuilder.EnableSensitiveDataLogging();
		optionsBuilder.UseApplicationServiceProvider(bindingContext);
		optionsBuilder.UseSqlite($"Data Source={dbPath}", s => s.UseNetTopologySuite());

		return new GalDataContext(optionsBuilder.Options);
	}
}
