using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuGet.Packaging;
using Prise;
using TradePath.Plugins.HostHelpers.Models;

namespace TradePath.Plugins.HostHelpers;

public partial class PluginManager(
	ILogger<PluginManager> logger,
	IOptions<PluginConfiguration> configuration,
	IPluginLoader pluginLoader
	): IPluginManager
{
	/// <inheritdoc />
	public async Task<IReadOnlyCollection<PluginDescriptor>> ListPluginsAsync(CancellationToken cancellationToken = default)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	public async Task<IReadOnlyCollection<PluginDescriptor>> FindPluginsAsync<T>(CancellationToken cancellationToken = default)
	{
		var scanResults = await pluginLoader.FindPlugins<T>(Path.Combine(configuration.Value.PluginFolder, "install"));
		
		var pluginDescriptors = new List<PluginDescriptor>();
		foreach (var scanResult in scanResults)
		{
			var directory = Path.GetDirectoryName(scanResult.AssemblyPath);
			while (true)
			{
				directory = Path.GetDirectoryName(directory);

				if (directory == null)
				{
					throw new InvalidOperationException("Invalid plugin directory structure");
				}
				
				if (Directory.EnumerateFiles(directory, "*.nuspec").Any())
				{
					//At the top level, break
					break;
				}
			}

			var reader = new PackageFolderReader(directory);
			var packageIdentity = await reader.GetIdentityAsync(cancellationToken);
			
			pluginDescriptors.Add(new PluginDescriptor((PluginName)packageIdentity.Id, packageIdentity.Version));
		}
		
		return pluginDescriptors;
	}

	/// <inheritdoc />
	public async Task<T> LoadPluginAsync<T>(PluginDescriptor plugin, CancellationToken cancellationToken = default)
	{
		throw new NotImplementedException();
	}
}
