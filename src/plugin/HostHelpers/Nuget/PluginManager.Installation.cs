using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Xml;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Logging;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Packaging.Signing;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;
using NuGet.Versioning;
using TradePath.Plugins.HostHelpers.Models;
using TradePath.Plugins.HostHelpers.Nuget;
using ILogger = NuGet.Common.ILogger;

// ReSharper disable once CheckNamespace
namespace TradePath.Plugins.HostHelpers;

public partial class PluginManager
{
	private readonly Meter _installationMeter =
		meterFactory.Create("tradepath.plugins.host.installation", Telemetry.InformationalVersion);


	private readonly Lazy<ISettings> _settings = new(() =>
	{
		using var act = Telemetry.ActivitySource.StartActivityWithParent();
		var settingsPath = Path.Combine(configuration.Value.PluginFolder, "nuget.config");

		if (!File.Exists(settingsPath))
		{
			act?.AddEvent(new ActivityEvent("WritingDefaultConfigFile"));
			LogMessages.WritingDefaultNugetSettingsFile(logger, settingsPath);
			using var file = File.Create(settingsPath);
			using var writer = XmlWriter.Create(file);
			writer.WriteStartDocument();
			writer.WriteStartElement("configuration");
			writer.WriteStartElement("packageSources");
			writer.WriteElementString("clear", string.Empty);
			writer.WriteStartElement("add");
			writer.WriteAttributeString("key", "nuget.org");
			writer.WriteAttributeString("value", "https://api.nuget.org/v3/index.json");
			writer.WriteEndElement();
			writer.WriteEndElement();
			writer.WriteStartElement("config");
			writer.WriteStartElement("add");
			writer.WriteAttributeString("key", "dependencyVersion");
			writer.WriteAttributeString("value", "HighestMinor");
			writer.WriteEndElement();
			writer.WriteStartElement("add");
			writer.WriteAttributeString("key", "globalPackagesFolder");
			writer.WriteAttributeString("value", Path.Combine(configuration.Value.PluginFolder, "pkg"));
			writer.WriteEndElement();
			writer.WriteEndElement();
			writer.WriteEndElement();
			writer.WriteEndDocument();

			writer.Flush();
			file.Flush();
		}

		LogMessages.AttachedSettingsObjectToFile(logger, settingsPath);
		return new Settings(configuration.Value.PluginFolder, "nuget.config");
	});

	private UpDownCounter<int>? _packagesInstalled;

	private UpDownCounter<int>? _pluginsInstalled;

	private ISourceRepositoryProvider? _sourceRepositoryProvider;

	private UpDownCounter<int> PackagesInstalled =>
		_packagesInstalled ??= _installationMeter.CreateUpDownCounter<int>(
			_installationMeter.Name + ".package.count",
			"{package}",
			"Number of individual NuGet packages installed"
		);

	private UpDownCounter<int> PluginsInstalled =>
		_pluginsInstalled ??= _installationMeter.CreateUpDownCounter<int>(
			_installationMeter.Name + ".plugin.count",
			"{plugin}",
			"Number of plugins installed"
		);

	private ISettings Settings => _settings.Value;

	private ISourceRepositoryProvider SourceRepositoryProvider => _sourceRepositoryProvider ??=
		new CachingSourceProvider(new PackageSourceProvider(Settings));


	/// <inheritdoc />
	public async Task InstallPluginAsync(
		PluginInstallConfiguration plugin,
		IProgress<PluginInstallationProgress>? progress = null,
		CancellationToken cancellationToken = default
	)
	{
		using var act = Telemetry.ActivitySource.StartActivityWithParent();
		using (logger.BeginScope(plugin))
		{
			LogMessages.BeginPluginInstallation(logger, plugin.Name.Value);
			var nugetLogger = new NugetLoggingAdaptor(logger);

			using var sourceCacheContext = new SourceCacheContext();

			var packageId = await GetPackageIdentityAsync(
				plugin,
				sourceCacheContext,
				SourceRepositoryProvider.GetRepositories(),
				cancellationToken
			);

			if (packageId is null)
			{
				LogMessages.UnableToLocatePackage(logger, plugin.Name.Value,
					plugin.VersionRange?.ToNormalizedString() ?? "null", plugin.PreRelease);
				throw new InvalidOperationException(
					$"Unable to find package {plugin.Name} with version {plugin.VersionRange}");
			}

			LogMessages.LocatedPackage(logger, packageId);
			using (logger.BeginScope(packageId))
			{
				var targetFramework = NuGetFramework.ParseFolder("netcoreapp3.1");
				var allPackages = new HashSet<SourcePackageDependencyInfo>();

				LogMessages.FetchingDependenciesForPackage(logger, packageId);
				await GetPackageDependencies(
					packageId,
					sourceCacheContext,
					targetFramework,
					nugetLogger,
					SourceRepositoryProvider.GetRepositories().ToArray(),
					DependencyContext.Default!,
					allPackages,
					progress,
					cancellationToken
				);

				LogMessages.ResolveDependencyTree(logger);
				progress?.Report(new PluginInstallationProgress(null, null, true));
				var packagesToInstall = GetPackagesToInstall(
					SourceRepositoryProvider,
					nugetLogger,
					[plugin],
					allPackages
				).ToArray();

				var packageDirectory = Path.Combine(configuration.Value.PluginFolder, "install");

				LogMessages.InstallingPackages(logger, packageDirectory);
				await InstallPackages(sourceCacheContext, nugetLogger, packagesToInstall, packageDirectory, progress,
					cancellationToken);

				PluginsInstalled.Add(1);

				LogMessages.PluginInstalled(logger);
			}
		}
	}

	private async Task InstallPackages(
		SourceCacheContext sourceCacheContext,
		ILogger nLogger,
		IReadOnlyCollection<SourcePackageDependencyInfo> packagesToInstall,
		string rootPackagesDirectory,
		IProgress<PluginInstallationProgress>? progress,
		CancellationToken cancellationToken
	)
	{
		using var act = Telemetry.ActivitySource.StartActivityWithParent();
		var packagePathResolver = new PackagePathResolver(rootPackagesDirectory);
		var packageExtractionContext = new PackageExtractionContext(
			PackageSaveMode.Files | PackageSaveMode.Nuspec,
			XmlDocFileSaveMode.Skip,
			ClientPolicyContext.GetClientPolicy(Settings, nLogger),
			nLogger
		);

		progress?.Report(new PluginInstallationProgress(null, null, false,
			packagesToInstall.ToDictionary(x => x.Id, _ => 0)
		));

		foreach (var package in packagesToInstall)
		{
			LogMessages.InstallingPackage(logger, new PackageIdentity(package.Id, package.Version));
			string source;
			PackageReaderBase packageReader = null!;
			using (logger.BeginScope(new PackageIdentity(package.Id, package.Version)))
			{
				try
				{
					progress?.Report(new PluginInstallationProgress(InstallationTasks: new Dictionary<string, int>
					{
						{ package.Id, 1 }
					}));
					var installedPath = packagePathResolver.GetInstalledPath(package);
					if (installedPath == null)
					{
						progress?.Report(new PluginInstallationProgress(InstallationTasks: new Dictionary<string, int>
						{
							{ package.Id, 2 }
						}));

						LogMessages.DownloadingPackage(logger, package.DownloadUri ?? package.Source.PackageSource.SourceUri);
						var downloadResource =
							await package.Source.GetResourceAsync<DownloadResource>(cancellationToken);

						// Download the package (might come from the shared package cache).
						var downloadResult = await downloadResource.GetDownloadResourceResultAsync(
							package,
							new PackageDownloadContext(sourceCacheContext),
							SettingsUtility.GetGlobalPackagesFolder(Settings),
							nLogger,
							cancellationToken
						);

						source = downloadResult.PackageSource;
						packageReader = downloadResult.PackageReader;
					}
					else
					{
						LogMessages.UsingCachedPackageSource(logger);
						packageReader = new PackageFolderReader(installedPath);
						source = installedPath;
					}

					progress?.Report(new PluginInstallationProgress(InstallationTasks: new Dictionary<string, int>
					{
						{ package.Id, 3 }
					}));

					LogMessages.ExtractingPackageContents(logger,
						packagePathResolver.GetInstallPath(new PackageIdentity(package.Id, package.Version)));

					// Extract the package into the target directory.
					await PackageExtractor.ExtractPackageAsync(
						source,
						packageReader,
						packagePathResolver,
						packageExtractionContext,
						cancellationToken
					);

					PackagesInstalled?.Add(1, new KeyValuePair<string, object?>[]
					{
						new("package.id", package.Id),
						new("package.version", package.Version.ToString()),
						new("package.source", source)
					});

					progress?.Report(new PluginInstallationProgress(InstallationTasks: new Dictionary<string, int>
					{
						{ package.Id, 4 }
					}));
					LogMessages.PackageInstalled(logger);
				}
				finally
				{
					packageReader?.Dispose();
				}
			}
		}
	}

	/// <summary>
	///     Simplify the list of packages to a set that are all compatible with each other, the host and without duplicates.
	/// </summary>
	private IEnumerable<SourcePackageDependencyInfo> GetPackagesToInstall(
		ISourceRepositoryProvider sourceRepositoryProvider,
		ILogger nLogger,
		IEnumerable<PluginInstallConfiguration> plugins,
		HashSet<SourcePackageDependencyInfo> allPackages
	)
	{
		using var act = Telemetry.ActivitySource.StartActivityWithParent();
		if (!Enum.TryParse<DependencyBehavior>(SettingsUtility.GetConfigValue(Settings, "dependencyVersion"),
			    out var dependencyBehavior))
		{
			dependencyBehavior = DependencyBehavior.HighestMinor;
		}


		// Create a package resolver context.
		var resolverContext = new PackageResolverContext(
			dependencyBehavior,
			plugins.Select(x => x.Name.Value),
			[],
			[],
			[],
			allPackages,
			sourceRepositoryProvider.GetRepositories().Select(s => s.PackageSource),
			nLogger
		);
		LogMessages.ConfiguredPackageResolution(logger, dependencyBehavior);

		var resolver = new PackageResolver();

		// Work out the actual set of packages to install.
		var packagesToInstall = resolver
			.Resolve(resolverContext, CancellationToken.None)
			.Select(p => allPackages
				.Single(x => PackageIdentityComparer.Default.Equals(x, p))
			);
		return packagesToInstall;
	}

	/// <summary>
	///     Gets the dependencies for a package.
	/// </summary>
	private async Task GetPackageDependencies(
		PackageIdentity package,
		SourceCacheContext cacheContext,
		NuGetFramework framework,
		ILogger nLogger,
		ICollection<SourceRepository> repositories,
		DependencyContext hostDependencies,
		ISet<SourcePackageDependencyInfo> availablePackages,
		IProgress<PluginInstallationProgress>? progress,
		CancellationToken cancelToken
	)
	{
		using var act = Telemetry.ActivitySource.StartActivityWithParent();

		LogMessages.ResolvingDependencies(logger, package);

		// Don't recurse over a package we've already seen.
		if (availablePackages.Contains(package))
		{
			LogMessages.SkippingAlreadyResolvedPackage(logger, package);
			return;
		}

		foreach (var sourceRepository in repositories)
		{
			act?.AddEvent(new ActivityEvent("QueryingRepository", default, new ActivityTagsCollection
			{
				{ "plugin.repository.type", sourceRepository.FeedTypeOverride.ToString() },
				{ "plugin.repository.name", sourceRepository.PackageSource.Name },
				{ "plugin.repository.url", sourceRepository.PackageSource.Source }
			}));
			// Get the dependency info for the package.
			var dependencyInfoResource = await sourceRepository.GetResourceAsync<DependencyInfoResource>(cancelToken);
			var dependencyInfo = await dependencyInfoResource.ResolvePackage(
				package,
				framework,
				cacheContext,
				nLogger,
				cancelToken);

			// No info for the package in this repository.
			if (dependencyInfo == null)
			{
				LogMessages.NoDependencyInfoInRepository(logger, package, sourceRepository.PackageSource);
				continue;
			}

			LogMessages.FoundDependencyInfoInRepository(logger, package, sourceRepository.PackageSource,
				dependencyInfo.Dependencies.Count());

			// Filter the dependency info.
			// Don't bring in any dependencies that are provided by the host.
			var actualSourceDep = new SourcePackageDependencyInfo(
				dependencyInfo.Id,
				dependencyInfo.Version,
				dependencyInfo.Dependencies.Where(dep => !DependencySuppliedByHost(hostDependencies, dep)),
				dependencyInfo.Listed,
				dependencyInfo.Source);

			availablePackages.Add(actualSourceDep);

			act?.AddEvent(new ActivityEvent("ResolvedPackageDependencies", default, new ActivityTagsCollection
			{
				{ "plugin.repository.type", actualSourceDep.Source.FeedTypeOverride.ToString() },
				{ "plugin.repository.name", actualSourceDep.Source.PackageSource.Name },
				{ "plugin.repository.url", actualSourceDep.Source.PackageSource.Source },
				{ "plugin.id", actualSourceDep.Id },
				{ "plugin.version", actualSourceDep.Version.ToString() }
			}));

			LogMessages.ResolvingChildDependencies(logger, package);
			// Recurse through each package.
			foreach (var dependency in actualSourceDep.Dependencies)
			{
				act?.AddEvent(new ActivityEvent("ResolvingDependencyDependencies", default, new ActivityTagsCollection
				{
					{ "plugin.repository.type", actualSourceDep.Source.FeedTypeOverride.ToString() },
					{ "plugin.repository.name", actualSourceDep.Source.PackageSource.Name },
					{ "plugin.repository.url", actualSourceDep.Source.PackageSource.Source },
					{ "plugin.id", actualSourceDep.Id },
					{ "plugin.version", actualSourceDep.Version.ToString() },
					{ "plugin.dependency.id", dependency.Id },
					{ "plugin.dependency.version", dependency.VersionRange.ToString() }
				}));

				progress?.Report(new PluginInstallationProgress(new Dictionary<string, string>
				{
					{ actualSourceDep.Id, dependency.Id }
				}));
				await GetPackageDependencies(
					new PackageIdentity(dependency.Id, dependency.VersionRange.MinVersion),
					cacheContext,
					framework,
					nLogger,
					repositories,
					hostDependencies,
					availablePackages,
					progress,
					cancelToken
				);

				progress?.Report(new PluginInstallationProgress(null, new List<string>
				{
					actualSourceDep.Id
				}));
			}

			break;
		}
	}

	private bool DependencySuppliedByHost(DependencyContext hostDependencies, PackageDependency dep)
	{
		using var act = Telemetry.ActivitySource.StartActivityWithParent();

		if (RuntimeProvidedPackages.IsPackageProvidedByRuntime(dep.Id))
		{
			LogMessages.RequestedPackageIsProvidedByRuntime(logger, dep);
			return true;
		}

		// See if a runtime library with the same ID as the package is available in the host's runtime libraries.
		var runtimeLib = hostDependencies.RuntimeLibraries.FirstOrDefault(r => r.Name == dep.Id);

		if (runtimeLib is not null)
		{
			// What version of the library is the host using?
			var parsedLibVersion = NuGetVersion.Parse(runtimeLib.Version);

			var hostProvides = parsedLibVersion.IsPrerelease ||
			                   // Always use pre-release versions from the host, otherwise it becomes
			                   // a nightmare to develop across multiple active versions.
			                   // Does the host version satisfy the version range of the requested package?
			                   // If so, we can provide it; otherwise, we cannot.
			                   dep.VersionRange.Satisfies(parsedLibVersion);

			if (hostProvides)
			{
				LogMessages.RequestedPackageIsProvidedByHost(logger, dep);
			}

			return hostProvides;
		}

		LogMessages.RequestedPackageIsNotProvided(logger, dep);

		return false;
	}

	private async Task<PackageIdentity?> GetPackageIdentityAsync(
		PluginInstallConfiguration config,
		SourceCacheContext cache,
		IEnumerable<SourceRepository> repositories,
		CancellationToken cancelToken
	)
	{
		using var act = Telemetry.ActivitySource.StartActivityWithParent();
		LogMessages.ResolvingToPackage(logger, config);
		foreach (var sourceRepository in repositories)
		{
			act?.AddEvent(new ActivityEvent("QueryingRepository", default, new ActivityTagsCollection
			{
				{ "plugin.repository.type", sourceRepository.FeedTypeOverride.ToString() },
				{ "plugin.repository.name", sourceRepository.PackageSource.Name },
				{ "plugin.repository.url", sourceRepository.PackageSource.Source }
			}));
			var findPackageResource = await sourceRepository.GetResourceAsync<FindPackageByIdResource>(cancelToken);

			var allVersions = await findPackageResource.GetAllVersionsAsync(
				config.Name.Value,
				cache,
				new NugetLoggingAdaptor(logger),
				cancelToken
			);

			NuGetVersion? selected;

			if (config.VersionRange != null)
			{
				LogMessages.MatchingBestVersion(logger, config.VersionRange, config.PreRelease);
				var bestVersion = config.VersionRange
					.FindBestMatch(allVersions.Where(v => config.PreRelease || !v.IsPrerelease));

				selected = bestVersion;
			}
			else
			{
				LogMessages.MatchingBestVersion(logger, config.PreRelease);
				selected = allVersions.LastOrDefault(v => v.IsPrerelease == config.PreRelease);
			}

			if (selected != null)
			{
				LogMessages.FoundMatchingPackage(logger, config);
				return new PackageIdentity(config.Name.Value, selected);
			}

			LogMessages.PackageNotFoundInRepository(logger, config, sourceRepository.PackageSource);
		}

		LogMessages.PackageNotFound(logger, config);
		return null;
	}

	private static partial class LogMessages
	{
		[LoggerMessage(LogLevel.Debug,
			"Configured package resolution with transient dependency behaviour {DependencyBehavior}")]
		internal static partial void ConfiguredPackageResolution(
			ILogger<PluginManager> logger,
			DependencyBehavior dependencyBehavior
		);

		[LoggerMessage(LogLevel.Trace, "Plugin installed successfully")]
		internal static partial void PluginInstalled(
			ILogger<PluginManager> logger
		);

		[LoggerMessage(LogLevel.Trace, "Package installed successfully")]
		internal static partial void PackageInstalled(
			ILogger<PluginManager> logger
		);

		[LoggerMessage(LogLevel.Trace, "Extracting package contents to {DestinationPath}")]
		internal static partial void ExtractingPackageContents(
			ILogger<PluginManager> logger,
			string destinationPath
		);

		[LoggerMessage(LogLevel.Trace, "Using cached package resource")]
		internal static partial void UsingCachedPackageSource(ILogger<PluginManager> logger);

		[LoggerMessage(LogLevel.Trace, "Downloading package from {Uri}")]
		internal static partial void DownloadingPackage(ILogger<PluginManager> logger, Uri uri);

		[LoggerMessage(LogLevel.Debug, "Installing package {PackageIdentity}")]
		internal static partial void InstallingPackage(ILogger<PluginManager> logger, PackageIdentity packageIdentity);

		[LoggerMessage(LogLevel.Information, "Installing packages in {PackageDirectory}")]
		internal static partial void InstallingPackages(ILogger<PluginManager> logger, string packageDirectory);

		[LoggerMessage(LogLevel.Debug, "Resolving dependency tree")]
		internal static partial void ResolveDependencyTree(ILogger<PluginManager> logger);

		[LoggerMessage(LogLevel.Information, "Resolving dependencies for package {PackageId}")]
		internal static partial void FetchingDependenciesForPackage(ILogger<PluginManager> logger,
			PackageIdentity packageId);

		[LoggerMessage(LogLevel.Information, "Located package with ID {PackageId}")]
		internal static partial void LocatedPackage(
			ILogger<PluginManager> logger,
			PackageIdentity packageId
		);

		[LoggerMessage(LogLevel.Error,
			"Unable to locate package {PackageId} within version range {VersionRange}, pre-release enabled? {PreRelease}")]
		internal static partial void UnableToLocatePackage(
			ILogger<PluginManager> logger,
			string packageId,
			string versionRange,
			bool preRelease
		);

		[LoggerMessage(LogLevel.Information, "Installing plugin {PluginId}")]
		internal static partial void BeginPluginInstallation(ILogger<PluginManager> logger, string pluginId);

		[LoggerMessage(LogLevel.Debug, "Writing default NuGet settings file at {SettingsPath}")]
		internal static partial void
			WritingDefaultNugetSettingsFile(ILogger<PluginManager> logger, string settingsPath);

		[LoggerMessage(LogLevel.Debug, "Attached NuGet settings object to file at {SettingsPath}")]
		internal static partial void AttachedSettingsObjectToFile(ILogger<PluginManager> logger, string settingsPath);

		[LoggerMessage(LogLevel.Trace, "Skipping already resolved package {PackageId}")]
		internal static partial void SkippingAlreadyResolvedPackage(ILogger<PluginManager> logger,
			PackageIdentity packageId);

		[LoggerMessage(LogLevel.Trace, "No dependency info found for {PackageId} in {SourceRepository}")]
		internal static partial void NoDependencyInfoInRepository(
			ILogger<PluginManager> logger,
			PackageIdentity packageId,
			PackageSource sourceRepository
		);

		[LoggerMessage(LogLevel.Trace, "Resolved {DependencyCount} dependencies for {PackageId} in {SourceRepository}")]
		internal static partial void FoundDependencyInfoInRepository(
			ILogger<PluginManager> logger,
			PackageIdentity packageId,
			PackageSource sourceRepository,
			int dependencyCount
		);

		[LoggerMessage(LogLevel.Trace, "Resolving child dependencies for {PackageId}")]
		internal static partial void ResolvingChildDependencies(
			ILogger<PluginManager> logger,
			PackageIdentity packageId
		);

		[LoggerMessage(LogLevel.Trace, "Resolving dependencies for {PackageId}")]
		internal static partial void ResolvingDependencies(
			ILogger<PluginManager> logger,
			PackageIdentity packageId
		);

		[LoggerMessage(LogLevel.Trace, "Requested package {PackageId} is provided by the runtime")]
		internal static partial void RequestedPackageIsProvidedByRuntime(
			ILogger<PluginManager> logger,
			PackageDependency packageId
		);

		[LoggerMessage(LogLevel.Trace, "Requested package {PackageId} is provided by the host")]
		internal static partial void RequestedPackageIsProvidedByHost(
			ILogger<PluginManager> logger,
			PackageDependency packageId
		);

		[LoggerMessage(LogLevel.Trace,
			"Requested package {PackageId} is not provided and must be installed from NuGet")]
		internal static partial void RequestedPackageIsNotProvided(
			ILogger<PluginManager> logger,
			PackageDependency packageId
		);

		[LoggerMessage(LogLevel.Debug, "Resolving install configuration {InstallConfiguration} to package identity")]
		internal static partial void ResolvingToPackage(
			ILogger<PluginManager> logger,
			PluginInstallConfiguration installConfiguration
		);

		[LoggerMessage(LogLevel.Trace,
			"Package matching install configuration {InstallConfiguration} not found in {SourceRepository}")]
		internal static partial void PackageNotFoundInRepository(
			ILogger<PluginManager> logger,
			PluginInstallConfiguration installConfiguration,
			PackageSource sourceRepository
		);

		[LoggerMessage(LogLevel.Warning, "Package matching install configuration {InstallConfiguration} not found")]
		internal static partial void PackageNotFound(
			ILogger<PluginManager> logger,
			PluginInstallConfiguration installConfiguration
		);

		[LoggerMessage(LogLevel.Debug, "Found package matching install configuration {InstallConfiguration}")]
		internal static partial void FoundMatchingPackage(
			ILogger<PluginManager> logger,
			PluginInstallConfiguration installConfiguration
		);

		[LoggerMessage(LogLevel.Trace,
			"Attempting to find latest version within {VersionRange}, including pre-release? {PreRelease}")]
		internal static partial void MatchingBestVersion(
			ILogger<PluginManager> logger,
			VersionRange versionRange,
			bool preRelease
		);

		[LoggerMessage(LogLevel.Trace, "Attempting to find latest version, including pre-release? {PreRelease}")]
		internal static partial void MatchingBestVersion(
			ILogger<PluginManager> logger,
			bool preRelease
		);
	}
}
