using NuGet.Packaging.Core;

namespace TradePath.Plugins.HostHelpers.Nuget;

internal static class PackageTypes
{
	public static PackageType Plugin => new("TradePath.Plugin", new Version(1, 0));
}
