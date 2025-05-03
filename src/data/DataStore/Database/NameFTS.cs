namespace TradePath.DataStore.Database;

public class NameFts<TKey, TEntity> where TKey: notnull
{
	public required TKey Id { get; set; }
	
	public required TKey EntityId { get; set; }
	
	public TEntity? Entity { get; set; }

	// public required string Name { get; set; }

	public string? Match { get; set; }
	public double? Rank { get; set; }
}
