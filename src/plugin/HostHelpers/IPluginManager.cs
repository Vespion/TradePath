using Prise;
using TradePath.Plugins.HostHelpers.Models;

namespace TradePath.Plugins.HostHelpers;

/// <summary>
///     Interface for allowing a host to manage its plugins.
/// </summary>
public interface IPluginManager
{
	/// <summary>
	///     Installs a plugin
	/// </summary>
	Task InstallPluginAsync(PluginInstallConfiguration plugin, IProgress<PluginInstallationProgress>? progress = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	///     Lists installed plugins
	/// </summary>
	Task<IReadOnlyCollection<PluginDescriptor>> ListPluginsAsync(CancellationToken cancellationToken = default);

	/// <summary>
	///     Finds plugins that implement the specified type.
	/// </summary>
	Task<IReadOnlyCollection<PluginLoadingDescriptor>> FindPluginsAsync<T>(
		CancellationToken cancellationToken = default);

	/// <summary>
	///     Loads a plugin that implements the specified type. Requires the results of <see cref="FindPluginsAsync{T}" /> to be
	///     passed in.
	/// </summary>
	Task<T> LoadPluginAsync<T>(PluginLoadingDescriptor plugin, Action<PluginLoadContext>? configure = null,
		CancellationToken cancellationToken = default);
}
