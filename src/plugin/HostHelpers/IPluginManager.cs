using Prise;
using TradePath.Plugins.HostHelpers.Models;

namespace TradePath.Plugins.HostHelpers;

public interface IPluginManager
{ 
	Task InstallPluginAsync(PluginInstallConfiguration plugin, IProgress<PluginInstallationProgress>? progress = null, CancellationToken cancellationToken = default);
	
	Task<IReadOnlyCollection<PluginDescriptor>> ListPluginsAsync(CancellationToken cancellationToken = default);
	
	Task<IReadOnlyCollection<PluginLoadingDescriptor>> FindPluginsAsync<T>(CancellationToken cancellationToken = default);
	
	Task<T> LoadPluginAsync<T>(PluginLoadingDescriptor plugin, Action<PluginLoadContext>? configure = null, CancellationToken cancellationToken = default);
}
