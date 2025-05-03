using Microsoft.Extensions.Logging;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.PackageManagement;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.ProjectModel;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;
using TradePath.Plugins.HostHelpers.Nuget;

namespace TradePath.Plugins.HostHelpers;

public class PluginManager: IPluginManager
{
	private readonly ILogger<PluginManager> _logger;
	private readonly ISettings _nugetSettings;
	
	private readonly ISourceRepositoryProvider _sourceRepositoryProvider;
	
	private readonly NuGetPackageManager _packageManager;
	
	private readonly PluginProject _project;

	public PluginManager(string pluginsFolder, ILogger<PluginManager> logger)
	{
		_logger = logger;
		_project = new PluginProject(pluginsFolder);

		_nugetSettings = new Settings(pluginsFolder, "nuget.config");

		_sourceRepositoryProvider = new CachingSourceProvider(new PackageSourceProvider(_nugetSettings));
		
		_packageManager = new NuGetPackageManager(
			_sourceRepositoryProvider,
			_nugetSettings,
			Path.Combine(pluginsFolder, "pkg")
		)
		{
			PackagesFolderNuGetProject = _project
		};
	}

	public async Task InstallPlugin(PackageIdentity pluginId, CancellationToken cancellationToken = default)
	{
		using var _ = _logger.BeginScope(pluginId);
		var allowPrerelease = false;
		var allowUnlisted = false;
		var dependencyBehavior = DependencyBehavior.Lowest;
		var versionConstraints = VersionConstraints.None;
		
		foreach (var settingItem in _nugetSettings.GetSection("resolution")?.Items ?? [])
		{
			
		}
		
		var resolutionContext = new ResolutionContext(
			dependencyBehavior,
			allowPrerelease,
			allowUnlisted,
			versionConstraints
		);

		var context = new PluginProjectContext(_logger, _nugetSettings);

		try
		{
			await _packageManager.InstallPackageAsync(
				_project,
				pluginId,
				resolutionContext,
				context,
				_sourceRepositoryProvider.GetRepositories(),
				[],
				cancellationToken
			);
		}
		catch (Exception ex)
		{
			throw;
		}
	}
}
