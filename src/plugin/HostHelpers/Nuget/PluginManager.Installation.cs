using System.Xml;
using Microsoft.Extensions.DependencyModel;
using NuGet.Common;
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

// ReSharper disable once CheckNamespace
namespace TradePath.Plugins.HostHelpers;

public partial class PluginManager
{
	private ISettings Settings
	{
		get => _settings.Value;
	}

	private ISourceRepositoryProvider SourceRepositoryProvider => _sourceRepositoryProvider ??=
		new CachingSourceProvider(new PackageSourceProvider(Settings));


	private readonly Lazy<ISettings> _settings = new(() =>
	{
		using var act = Telemetry.ActivitySource.StartActivity();
		var settingsPath = Path.Combine(configuration.Value.PluginFolder, "nuget.config");

		if (!File.Exists(settingsPath))
		{
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

		return new Settings(configuration.Value.PluginFolder, "nuget.config");
	});

	private ISourceRepositoryProvider? _sourceRepositoryProvider;


	/// <inheritdoc />
	public async Task InstallPluginAsync(PluginInstallConfiguration plugin,
		CancellationToken cancellationToken = default)
	{
		using var act = Telemetry.ActivitySource.StartActivity();
		using (logger.BeginScope(plugin))
		{
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
				throw new InvalidOperationException(
					$"Unable to find package {plugin.Name} with version {plugin.VersionRange}");
			}

			using (logger.BeginScope(packageId))
			{
				var targetFramework = NuGetFramework.ParseFolder("netcoreapp3.1");
				var allPackages = new HashSet<SourcePackageDependencyInfo>();
				
				await GetPackageDependencies(
					packageId,
					sourceCacheContext,
					targetFramework,
					nugetLogger,
					SourceRepositoryProvider.GetRepositories().ToArray(),
					DependencyContext.Default!,
					allPackages,
					cancellationToken
				);
 
				var packagesToInstall = GetPackagesToInstall(
					SourceRepositoryProvider,
					nugetLogger,
					[plugin],
					allPackages
				);
 
				var packageDirectory = Path.Combine(configuration.Value.PluginFolder, "install");
				
				await InstallPackages(sourceCacheContext, nugetLogger, packagesToInstall, packageDirectory, cancellationToken);
			}
		}
	}
	
	private async Task InstallPackages(
		SourceCacheContext sourceCacheContext,
		ILogger nLogger, 
		IEnumerable<SourcePackageDependencyInfo> packagesToInstall,
		string rootPackagesDirectory, 
		CancellationToken cancellationToken
	)
	{
		var packagePathResolver = new PackagePathResolver(rootPackagesDirectory);
		var packageExtractionContext = new PackageExtractionContext(
			PackageSaveMode.Files | PackageSaveMode.Nuspec,
			XmlDocFileSaveMode.Skip,
			ClientPolicyContext.GetClientPolicy(Settings, nLogger),
			nLogger
		);
		
		foreach (var package in packagesToInstall)
		{
			PackageReaderBase packageReader;
			var installedPath = packagePathResolver.GetInstalledPath(package);
			if (installedPath == null)
			{
				var downloadResource = await package.Source.GetResourceAsync<DownloadResource>(cancellationToken);
 
				// Download the package (might come from the shared package cache).
				var downloadResult = await downloadResource.GetDownloadResourceResultAsync(
					package,
					new PackageDownloadContext(sourceCacheContext),
					SettingsUtility.GetGlobalPackagesFolder(Settings),
					nLogger,
					cancellationToken
				);

				packageReader = downloadResult.PackageReader;
			}
			else
			{
				packageReader = new PackageFolderReader(installedPath);
			}
			
			// Extract the package into the target directory.
			await PackageExtractor.ExtractPackageAsync(
				"downloadResult.PackageSource",
				packageReader,
				packagePathResolver,
				packageExtractionContext,
				cancellationToken
			);
		}
	}
	
	/// <summary>
	/// Simplify the list of packages to a set that are all compatible with each other, the host and without duplicates.
	/// </summary>
	private IEnumerable<SourcePackageDependencyInfo> GetPackagesToInstall(
		ISourceRepositoryProvider sourceRepositoryProvider, 
		ILogger nLogger,
		IEnumerable<PluginInstallConfiguration> plugins, 
		HashSet<SourcePackageDependencyInfo> allPackages
		)
	{
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
	/// Gets the dependencies for a package.
	/// </summary>
	private async Task GetPackageDependencies(
		PackageIdentity package,
		SourceCacheContext cacheContext,
		NuGetFramework framework,
		ILogger nLogger,
		ICollection<SourceRepository> repositories,
		DependencyContext hostDependencies,
		ISet<SourcePackageDependencyInfo> availablePackages,
		CancellationToken cancelToken
	)
	{
		// Don't recurse over a package we've already seen.
		if (availablePackages.Contains(package))
		{
			return;
		}

		foreach (var sourceRepository in repositories)
		{
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
				continue;
			}


			// Filter the dependency info.
			// Don't bring in any dependencies that are provided by the host.
			var actualSourceDep = new SourcePackageDependencyInfo(
				dependencyInfo.Id,
				dependencyInfo.Version,
				dependencyInfo.Dependencies.Where(dep => !DependencySuppliedByHost(hostDependencies, dep)),
				dependencyInfo.Listed,
				dependencyInfo.Source);

			availablePackages.Add(actualSourceDep);

			// Recurse through each package.
			foreach (var dependency in actualSourceDep.Dependencies)
			{
				await GetPackageDependencies(
					new PackageIdentity(dependency.Id, dependency.VersionRange.MinVersion),
					cacheContext,
					framework,
					nLogger,
					repositories,
					hostDependencies,
					availablePackages,
					cancelToken
				);
			}

			break;
		}
	}
	
	private static bool DependencySuppliedByHost(DependencyContext hostDependencies, PackageDependency dep)
	{
		if(RuntimeProvidedPackages.IsPackageProvidedByRuntime(dep.Id))
		{
			return true;
		}
		
		// See if a runtime library with the same ID as the package is available in the host's runtime libraries.
		var runtimeLib = hostDependencies.RuntimeLibraries.FirstOrDefault(r => r.Name == dep.Id);
 
		if (runtimeLib is not null)
		{
			// What version of the library is the host using?
			var parsedLibVersion = NuGetVersion.Parse(runtimeLib.Version);

			return parsedLibVersion.IsPrerelease ||
			       // Always use pre-release versions from the host, otherwise it becomes
			       // a nightmare to develop across multiple active versions.
			       // Does the host version satisfy the version range of the requested package?
			       // If so, we can provide it; otherwise, we cannot.
			       dep.VersionRange.Satisfies(parsedLibVersion);
		}
 
		return false;
	}

	private async Task<PackageIdentity?> GetPackageIdentityAsync(
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
				new NugetLoggingAdaptor(logger),
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
}
