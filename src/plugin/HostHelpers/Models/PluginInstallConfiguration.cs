using NuGet.Versioning;

namespace TradePath.Plugins.HostHelpers.Models;

public readonly record struct PluginInstallConfiguration(
	PluginName Name,
	VersionRange? VersionRange,
	bool PreRelease
);
