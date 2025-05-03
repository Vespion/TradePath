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

namespace TradePath.Plugins.HostHelpers.Nuget.Bare;

public class PluginManager(string pluginFolder, ILogger<PluginManager> logger) : IPluginManager
{
	private readonly ISourceRepositoryProvider _sourceProvider = new CachingSourceProvider(
		new PackageSourceProvider(GetSettingsFile(pluginFolder))
	);

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

	public async Task InstallPlugin(PluginInstallConfiguration plugin, CancellationToken cancellationToken = default)
	{
		using var act = Telemetry.ActivitySource.StartActivity();
		var repositories = _sourceProvider.GetRepositories().ToArray();

		using var sourceCacheContext = new SourceCacheContext();

		var nugetLogger = new NugetLoggingAdaptor(logger);

		var packageId = await GetPackageIdentity(
			plugin,
			sourceCacheContext,
			nugetLogger,
			repositories,
			cancellationToken
		);

		if (packageId == null)
		{
			throw new Exception($"Plugin {plugin.Name} not found");
		}
		
		// The framework we're using.
		var targetFramework = NuGetFramework.ParseFolder("net9.0");
		var allPackages = new HashSet<SourcePackageDependencyInfo>();

		await GetPackageDependencies(
			packageId,
			sourceCacheContext,
			targetFramework,
			nugetLogger,
			repositories,
			DependencyContext.Default!,
			allPackages,
			cancellationToken
		);
		
		var packagesToInstall = GetPackagesToInstall(
			_sourceProvider,
			nugetLogger,
			[packageId],
			allPackages
		);
 
		var packageDirectory = Path.Combine(pluginFolder, "install");
 
		await InstallPackages(sourceCacheContext, nugetLogger, packagesToInstall, packageDirectory, GetSettingsFile(pluginFolder), cancellationToken);
	}
	
	private async Task InstallPackages(
		SourceCacheContext sourceCacheContext,
		NuGet.Common.ILogger nLogger, 
		IEnumerable<SourcePackageDependencyInfo> packagesToInstall,
		string rootPackagesDirectory, 
		ISettings nugetSettings,
		CancellationToken cancellationToken
	)
	{
		var packagePathResolver = new PackagePathResolver(rootPackagesDirectory, true);
		var packageExtractionContext = new PackageExtractionContext(
			PackageSaveMode.Files | PackageSaveMode.Nuspec,
			XmlDocFileSaveMode.Skip,
			ClientPolicyContext.GetClientPolicy(nugetSettings, nLogger),
			nLogger
		);
 
		foreach (var package in packagesToInstall)
		{
			var downloadResource = await package.Source.GetResourceAsync<DownloadResource>(cancellationToken);
 
			// Download the package (might come from the shared package cache).
			var downloadResult = await downloadResource.GetDownloadResourceResultAsync(
				package,
				new PackageDownloadContext(sourceCacheContext),
				SettingsUtility.GetGlobalPackagesFolder(nugetSettings),
				nLogger,
				cancellationToken
			);
 
			// Extract the package into the target directory.
			await PackageExtractor.ExtractPackageAsync(
				downloadResult.PackageSource,
				downloadResult.PackageStream,
				packagePathResolver,
				packageExtractionContext,
				cancellationToken
			);
		}
	}
	
	private IEnumerable<SourcePackageDependencyInfo> GetPackagesToInstall(
		ISourceRepositoryProvider sourceRepositoryProvider, 
		NuGet.Common.ILogger nLogger,
		IEnumerable<PackageIdentity> packageIds, 
		HashSet<SourcePackageDependencyInfo> allPackages
	)
	{
		// Create a package resolver context.
		var resolverContext = new PackageResolverContext(
			DependencyBehavior.HighestMinor,
			packageIds.Select(x => x.Id),
			[],
			[],
			[],
			allPackages,
			sourceRepositoryProvider.GetRepositories().Select(s => s.PackageSource),
			nLogger
			);
 
		var resolver = new PackageResolver();
 
		// Work out the actual set of packages to install.
		var packagesToInstall = resolver.Resolve(resolverContext, CancellationToken.None)
			.Select(p => allPackages.Single(x => PackageIdentityComparer.Default.Equals(x, p)));
		return packagesToInstall;
	}

	private async Task GetPackageDependencies(
		PackageIdentity package,
		SourceCacheContext cacheContext,
		NuGetFramework framework,
		NuGet.Common.ILogger nugetLogger,
		IReadOnlyCollection<SourceRepository> repositories,
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
				nugetLogger,
				cancelToken
			);

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
				dependencyInfo.Source
			);

			availablePackages.Add(actualSourceDep);

			// Recurse through each package.
			foreach (var dependency in actualSourceDep.Dependencies)
			{
				await GetPackageDependencies(
					new PackageIdentity(dependency.Id, dependency.VersionRange.MinVersion),
					cacheContext,
					framework,
					nugetLogger,
					repositories,
					hostDependencies,
					availablePackages,
					cancelToken);
			}

			break;
		}
	}

	private bool DependencySuppliedByHost(DependencyContext hostDependencies, PackageDependency dep)
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

			if (parsedLibVersion.IsPrerelease)
			{
				// Always use pre-release versions from the host, otherwise it becomes
				// a nightmare to develop across multiple active versions.
				return true;
			}

			// Does the host version satisfy the version range of the requested package?
			// If so, we can provide it; otherwise, we cannot.
			return dep.VersionRange.Satisfies(parsedLibVersion);
		}

		return false;
	}

	private async Task<PackageIdentity?> GetPackageIdentity(
		PluginInstallConfiguration config,
		SourceCacheContext cache,
		NuGet.Common.ILogger nugetLogger,
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
				nugetLogger,
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
