using NuGet.Versioning;

namespace TradePath.Plugins.HostHelpers.Models;

/// <summary>
///     Represents a plugin simplified plugin descriptor.
/// </summary>
/// <param name="Name">The name/ID of the plugin</param>
/// <param name="Version">The version of the plugin</param>
public readonly record struct PluginDescriptor(
	PluginName Name,
	NuGetVersion Version
);
