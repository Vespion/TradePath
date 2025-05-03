namespace TradePath.Plugins.HostHelpers;

public record PluginSpec(
	string Name,
	string Authors,
	string? License,
	bool SupportsSystemNavigationProvider
);
