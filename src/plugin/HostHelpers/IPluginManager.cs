using NuGet.Packaging.Core;
using TradePath.Plugins.HostHelpers.Nuget.Bare;

namespace TradePath.Plugins.HostHelpers;

public interface IPluginManager
{ 
	Task InstallPlugin(PluginInstallConfiguration plugin, CancellationToken cancellationToken = default);
}
