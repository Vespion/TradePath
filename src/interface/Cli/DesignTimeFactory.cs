using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TradePath.DataStore.Database;

namespace TradePath.Cli;

#if DEBUG
public class DesignTimeFactory: IDesignTimeDbContextFactory<GalDataContext>
{
	/// <inheritdoc />
	public GalDataContext CreateDbContext(string[] args)
	{
		var optionsBuilder = new DbContextOptionsBuilder<GalDataContext>();
		optionsBuilder.EnableDetailedErrors();
		optionsBuilder.EnableSensitiveDataLogging();
		optionsBuilder.UseSqlite("Data Source=:memory:", s => s.UseNetTopologySuite());

		return new GalDataContext(optionsBuilder.Options);
	}
}
#endif
