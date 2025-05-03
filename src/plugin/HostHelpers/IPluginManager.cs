using NuGet.Packaging.Core;

namespace TradePath.Plugins.HostHelpers;

public interface IPluginManager
{ 
	Task InstallPlugin(PackageIdentity pluginId, CancellationToken cancellationToken = default);
}
