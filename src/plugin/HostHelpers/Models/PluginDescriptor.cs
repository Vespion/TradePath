using NuGet.Versioning;

namespace TradePath.Plugins.HostHelpers.Models;

public readonly record struct PluginDescriptor(
	PluginName Name,
	NuGetVersion Version
);
