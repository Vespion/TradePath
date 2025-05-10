using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using NetTopologySuite.Geometries;
using TradePath.Models;
using Vogen;

namespace TradePath.DataStore.Database;

public class GalDataContext(DbContextOptions<GalDataContext> options) : DbContext(options)
{
	public DbSet<StarSystem> Systems { get; set; } = null!;

	public DbSet<Station> Stations { get; set; } = null!;

	public DbSet<NameFts<StationId, Station>> StationNameIndex { get; set; } = null!;

	public DbSet<NameFts<StarSystemId, StarSystem>> SystemNameIndex { get; set; } = null!;

	/// <inheritdoc />
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<StarSystem>(e =>
		{
			e.Property(p => p.LastModified)
				.HasDefaultValueSql("CURRENT_TIMESTAMP")
				.ValueGeneratedOnAddOrUpdate()
				.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Save);
		});

		modelBuilder.Entity<NameFts<StationId, Station>>(x =>
		{
			x.Property(fts => fts.Match).HasColumnName(nameof(StationNameIndex));
		});

		modelBuilder.Entity<NameFts<StationId, Station>>(x =>
		{
			x.Property(fts => fts.Match).HasColumnName(nameof(SystemNameIndex));
		});
	}

	/// <inheritdoc />
	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		base.OnConfiguring(optionsBuilder);

		// optionsBuilder.UseTriggers(t => t
		// 	.AddTrigger<NameIndexingTrigger>()
		// );
	}

	/// <inheritdoc />
	protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
	{
		base.ConfigureConventions(configurationBuilder);

		configurationBuilder
			.Properties<Point>()
			.HaveColumnType("POINTZ");

		configurationBuilder.RegisterAllInEfCoreVogenConverters();
	}
}

[EfCoreConverter<StarId>]
[EfCoreConverter<StarSystemId>]
[EfCoreConverter<StarSystemName>]
[EfCoreConverter<StationId>]
[EfCoreConverter<StationName>]
public static partial class EfCoreVogenConverters;
