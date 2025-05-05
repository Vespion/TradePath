using TradePath.Plugins.HostHelpers.Models;

namespace TradePath.Plugins.HostHelpers;

public interface IPluginManager
{ 
	Task InstallPluginAsync(PluginInstallConfiguration plugin, CancellationToken cancellationToken = default);
	
	Task<IReadOnlyCollection<PluginDescriptor>> ListPluginsAsync(CancellationToken cancellationToken = default);
	
	Task<IReadOnlyCollection<PluginDescriptor>> FindPluginsAsync<T>(CancellationToken cancellationToken = default);
	
	Task<T> LoadPluginAsync<T>(PluginDescriptor plugin, CancellationToken cancellationToken = default);
}
