using NuGet.Versioning;

namespace TradePath.Plugins.HostHelpers.Models;

/// <summary>
///     Represents the required information to install a plugin.
/// </summary>
/// <param name="Name">The name/ID of the plugin</param>
/// <param name="VersionRange">The acceptable range of versions, if null tries to install the latest</param>
/// <param name="PreRelease">True if pre-release version should be considered. False otherwise</param>
public readonly record struct PluginInstallConfiguration(
	PluginName Name,
	VersionRange? VersionRange,
	bool PreRelease
);
