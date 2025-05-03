using Microsoft.Extensions.Logging;
using NuGet.Configuration;
using NuGet.PackageManagement;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Packaging.Signing;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;
using NuGet.Versioning;

namespace TradePath.Plugins.HostHelpers.Nuget.Project;

public class PluginManager: IPluginManager
{
	private readonly ILoggerFactory _loggerFactory;
	private readonly ISettings _settings;
	private readonly NuGetPackageManager _packageManager;
	private readonly ISourceRepositoryProvider _sourceRepositoryProvider;

	private readonly PluginInstallationProject _project;
	
	public PluginManager(string pluginRoot, ILoggerFactory loggerFactory)
	{
		_loggerFactory = loggerFactory;
		_settings = GetSettingsFile(pluginRoot);
		_sourceRepositoryProvider = new CachingSourceProvider(new PackageSourceProvider(_settings));

		_packageManager = new NuGetPackageManager(
			_sourceRepositoryProvider,
			_settings,
			SettingsUtility.GetGlobalPackagesFolder(_settings)
		);
		
		_project = new PluginInstallationProject(pluginRoot);
	}
	
	
	public async Task InstallPlugin(PluginInstallConfiguration plugin, CancellationToken cancellationToken = default)
	{
		using var sourceCacheContext = new SourceCacheContext();

		var clientPolicy = ClientPolicyContext.GetClientPolicy(_settings,
			new NugetLoggingAdaptor(_loggerFactory.CreateLogger<ClientPolicyContext>()));
		
		var projectContext = new PluginInstallationContext(_loggerFactory.CreateLogger<PluginInstallationContext>())
		{
			PackageExtractionContext = new PackageExtractionContext(
				PackageSaveMode.Defaultv3,
				XmlDocFileSaveMode.Skip,
				clientPolicy, 
				new NugetLoggingAdaptor(_loggerFactory.CreateLogger<PackageExtractionContext>())
			)
		};
		
		var packageId = await GetPackageIdentity(
			plugin,
			sourceCacheContext,
			_sourceRepositoryProvider.GetRepositories(),
			cancellationToken
		);

		if (packageId is null)
		{
			throw new PackageNotFoundProtocolException(new PackageIdentity(plugin.Name.Value,
				plugin.VersionRange?.MinVersion));
		}

		if (!Enum.TryParse(SettingsUtility.GetConfigValue(_settings, "dependencyVersion"),
			    out DependencyBehavior dependencyBehavior))
		{
			dependencyBehavior = DependencyBehavior.HighestMinor;
		}
		
		var resolutionContext = new ResolutionContext(
			dependencyBehavior,
			plugin.PreRelease,
			true,
			VersionConstraints.None
		);
		
		var downloadContext = new PackageDownloadContext(resolutionContext.SourceCacheContext)
		{
			ParentId = projectContext.OperationId,
			ClientPolicyContext = clientPolicy
		};
		
		await _packageManager.InstallPackageAsync(
			_project,
			packageId,
			resolutionContext,
			projectContext,
			downloadContext,
			_sourceRepositoryProvider.GetRepositories(),
			[],
			cancellationToken
		);
	}
	
	private async Task<PackageIdentity?> GetPackageIdentity(
		PluginInstallConfiguration config,
		SourceCacheContext cache,
		IEnumerable<SourceRepository> repositories,
		CancellationToken cancelToken
	)
	{
		using var act = Telemetry.ActivitySource.StartActivity();
		foreach (var sourceRepository in repositories)
		{
			var findPackageResource = await sourceRepository.GetResourceAsync<FindPackageByIdResource>(cancelToken);

			var allVersions = await findPackageResource.GetAllVersionsAsync(
				config.Name.Value,
				cache,
				new NugetLoggingAdaptor(_loggerFactory.CreateLogger<FindPackageByIdResource>()),
				cancelToken
			);

			NuGetVersion? selected;

			if (config.VersionRange != null)
			{
				var bestVersion = config.VersionRange
					.FindBestMatch(allVersions.Where(v => config.PreRelease || !v.IsPrerelease));

				selected = bestVersion;
			}
			else
			{
				selected = allVersions.LastOrDefault(v => v.IsPrerelease == config.PreRelease);
			}

			if (selected != null)
			{
				return new PackageIdentity(config.Name.Value, selected);
			}
		}

		return null;
	}
	
	private static ISettings GetSettingsFile(string pluginsFolder)
	{
		using var act = Telemetry.ActivitySource.StartActivity();
		var settingsPath = Path.Combine(pluginsFolder, "nuget.config");

		if (!File.Exists(settingsPath))
		{
			using var stream =
				typeof(PluginManager).Assembly.GetManifestResourceStream(typeof(Resources.Plugins),
					"default.nuget.config");
			Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
			using var fileStream = File.OpenWrite(settingsPath);
			stream!.CopyTo(fileStream);
			fileStream.Flush();
		}

		return new Settings(pluginsFolder, "nuget.config");
	}
}
