using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Reflection;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using Prise;
using TradePath.Plugins.HostHelpers.Models;

namespace TradePath.Plugins.HostHelpers;

/// <summary>
///     Implements the <see cref="IPluginManager" /> interface.
/// </summary>
public partial class PluginManager(
	ILogger<PluginManager> logger,
	IMeterFactory meterFactory,
	IOptions<PluginConfiguration> configuration,
	IPluginLoader pluginLoader
) : IPluginManager
{
	/// <inheritdoc />
	public Task<IReadOnlyCollection<PluginDescriptor>> ListPluginsAsync(CancellationToken cancellationToken = default)
	{
		using var act = Telemetry.ActivitySource.StartActivityWithParent();
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	public async Task<IReadOnlyCollection<PluginLoadingDescriptor>> FindPluginsAsync<T>(
		CancellationToken cancellationToken = default)
	{
		using var act = Telemetry.ActivitySource.StartActivityWithParent();
		LogMessages.ScanningForPlugins(logger, configuration.Value.PluginFolder, typeof(T).Name);
		var scanResults = await pluginLoader.FindPlugins<T>(Path.Combine(configuration.Value.PluginFolder, "install"));

		var pluginDescriptors = new List<PluginLoadingDescriptor>();
		foreach (var scanResult in scanResults)
		{
			LogMessages.FoundPluginAssembly(logger, scanResult.AssemblyName, scanResult.AssemblyPath,
				scanResult.PluginType.Name);
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

			LogMessages.ResolvedPackageDirectoryForPlugin(logger, directory);
			using var reader = new PackageFolderReader(directory);
			var packageIdentity = await reader.GetIdentityAsync(cancellationToken);

			LogMessages.LoadedPluginPackageIdentity(logger, packageIdentity);
			pluginDescriptors.Add(new PluginLoadingDescriptor(
				new PluginDescriptor((PluginName)packageIdentity.Id, packageIdentity.Version),
				scanResult
			));
		}

		LogMessages.FinishedScanningForPlugins(logger, configuration.Value.PluginFolder, typeof(T).Name);
		return pluginDescriptors;
	}

	/// <inheritdoc />
	[RequiresAssemblyFiles]
	public async Task<T> LoadPluginAsync<T>(
		PluginLoadingDescriptor plugin,
		Action<PluginLoadContext>? configure = null,
		CancellationToken cancellationToken = default)
	{
		using var act = Telemetry.ActivitySource.StartActivityWithParent();
		LogMessages.LoadingPlugin(logger, plugin.Descriptor, plugin.ScanResult.AssemblyName,
			plugin.ScanResult.AssemblyPath);
		LogMessages.LoadingHostFramework(logger);
		var dependencyContext = DependencyContext.Load(Assembly.GetCallingAssembly());
		LogMessages.LoadedHostFramework(logger, dependencyContext?.Target.Framework);
		return await pluginLoader.LoadPlugin<T>(plugin.ScanResult, dependencyContext?.Target.Framework, configure);
	}

	private static partial class LogMessages
	{
		[LoggerMessage(LogLevel.Information,
			"Finished scanning for plugins in {PluginFolder} that implement {PluginTypeName}")]
		internal static partial void FinishedScanningForPlugins(
			ILogger<PluginManager> logger,
			string pluginFolder,
			string pluginTypeName
		);

		[LoggerMessage(LogLevel.Debug, "Loaded plugin with package identity {PackageIdentity}")]
		internal static partial void LoadedPluginPackageIdentity(
			ILogger<PluginManager> logger,
			PackageIdentity packageIdentity
		);

		[LoggerMessage(LogLevel.Trace, "Resolved package directory for plugin at {PackageDirectory}")]
		internal static partial void ResolvedPackageDirectoryForPlugin(
			ILogger<PluginManager> logger,
			string packageDirectory
		);

		[LoggerMessage(LogLevel.Debug,
			"Found plugin assembly {AssemblyName} at {AssemblyPath} implementing {PluginTypeName}")]
		internal static partial void FoundPluginAssembly(
			ILogger<PluginManager> logger,
			string assemblyName,
			string assemblyPath,
			string pluginTypeName
		);

		[LoggerMessage(LogLevel.Information, "Scanning for plugins in {PluginFolder} that implement {PluginTypeName}")]
		internal static partial void ScanningForPlugins(
			ILogger<PluginManager> logger,
			string pluginFolder,
			string pluginTypeName
		);

		[LoggerMessage(LogLevel.Debug, "Loading host framework")]
		internal static partial void LoadingHostFramework(
			ILogger<PluginManager> logger
		);

		[LoggerMessage(LogLevel.Debug, "Loaded host framework {HostFramework}")]
		internal static partial void LoadedHostFramework(
			ILogger<PluginManager> logger,
			string? hostFramework
		);

		[LoggerMessage(LogLevel.Information, "Loading {Plugin} in assembly {AssemblyName} at {AssemblyPath}")]
		internal static partial void LoadingPlugin(
			ILogger<PluginManager> logger,
			PluginDescriptor plugin,
			string assemblyName,
			string assemblyPath
		);
	}
}
