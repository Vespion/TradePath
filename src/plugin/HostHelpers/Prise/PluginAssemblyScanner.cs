using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NuGet.Frameworks;
using NuGet.Packaging;
using Prise;
using Prise.AssemblyScanning;
using Prise.Plugin;

namespace TradePath.Plugins.HostHelpers.Prise;

/// <summary>
///     Implements <see cref="IAssemblyScanner" /> to scan for plugins in a nuget installation directory structure.
/// </summary>
/// <inheritdoc />
public sealed partial class PluginAssemblyScanner(
	Func<string, IMetadataLoadContext> metadataLoadContextFactory,
	Func<IDirectoryTraverser> directoryTraverser,
	string targetFramework,
	ILogger<PluginAssemblyScanner> logger
) : DefaultAssemblyScanner(metadataLoadContextFactory, directoryTraverser)
{
	private readonly NuGetFramework _framework = NuGetFramework.ParseFolder(targetFramework);

	/// <inheritdoc />
	public override async Task<IEnumerable<AssemblyScanResult>> Scan(IAssemblyScannerOptions options)
	{
		using var act = Telemetry.ActivitySource.StartActivity();

		LogMessages.Scanning(
			logger,
			options.StartingPath,
			options.PluginType.Name
		);

		var frameworkReducer = new FrameworkReducer();

		var scanPaths = new List<string>();

		foreach (var pluginDirectory in directoryTraverser.TraverseDirectories(options.StartingPath))
		{
			LogMessages.AddingPackagesToScan(
				logger,
				pluginDirectory
			);
			using var packageReader = new PackageFolderReader(pluginDirectory);

			var newPaths = (await GetCompatibleFrameworkPaths(frameworkReducer, _framework, packageReader))
				.Select(s => Path.Combine(pluginDirectory, s))
				.ToArray();

			act?.AddEvent(new ActivityEvent("FoundAdditionalScanPaths", default, new ActivityTagsCollection
			{
				{ "paths", newPaths }
			}));
			LogMessages.FoundAdditionalScanPaths(
				logger,
				newPaths
			);

			scanPaths.AddRange(newPaths);
		}

		var scanResults = new List<AssemblyScanResult>(scanPaths.Count);

		foreach (var scanPath in scanPaths)
		{
			foreach (var implementationType in GetImplementationsOfTypeFromAssembly(options.PluginType, scanPath))
			{
				LogMessages.FoundUsableImplementation(
					logger,
					implementationType.Name,
					options.PluginType.FullName ?? options.PluginType.Name,
					scanPath
				);

				act?.AddEvent(new ActivityEvent("FoundUsableImplementation", default, new ActivityTagsCollection
				{
					{ "plugin.implementation.type", implementationType.Name },
					{ "plugin.contract.type", options.PluginType.FullName ?? options.PluginType.Name },
					{ "plugin.assembly.path", scanPath }
				}));

				scanResults.Add(new AssemblyScanResult
				{
					ContractType = options.PluginType,
					AssemblyName = Path.GetFileName(scanPath),
					AssemblyPath = Path.GetDirectoryName(scanPath),
					PluginType = implementationType
				});
			}
		}

		LogMessages.ScanComplete(
			logger,
			options.StartingPath,
			options.PluginType.Name
		);

		return scanResults;
	}

	private List<Type> GetImplementationsOfTypeFromAssembly(Type type, string assemblyFullPath)
	{
		using var act = Telemetry.ActivitySource.StartActivity();

		LogMessages.GettingTypeImplementationsFromAssembly(
			logger,
			type.Name,
			assemblyFullPath
		);

		LogMessages.CreateTypeContext(
			logger,
			assemblyFullPath
		);
		var metadataLoadContext = metadataLoadContextFactory(assemblyFullPath);
		var assemblyShim = metadataLoadContext.LoadFromAssemblyName(Path.GetFileNameWithoutExtension(assemblyFullPath));
		disposables.Add(metadataLoadContext);

		LogMessages.FilteringTypesFromContext(
			logger,
			assemblyFullPath
		);
		return assemblyShim.Types
			.Where<Type>(t => t.CustomAttributes
				.Any(c => c.AttributeType.Name == nameof(PluginAttribute)
				          && (c.NamedArguments.First(a => a.MemberName == "PluginType").TypedValue.Value as Type)
				          ?.Name == type.Name &&
				          (c.NamedArguments.First(a => a.MemberName == "PluginType").TypedValue.Value as Type)
				          ?.Namespace == type.Namespace))
			.OrderBy((Func<Type, string>)(t => t.Name))
			.ToList();
	}

	private async Task<string[]> GetCompatibleFrameworkPaths(
		FrameworkReducer frameworkReducer,
		NuGetFramework nuGetFramework,
		PackageReaderBase packageReader,
		CancellationToken cancellationToken = default
	)
	{
		using var act = Telemetry.ActivitySource.StartActivity();
		var id = await packageReader.GetIdentityAsync(cancellationToken);

		act?.AddTag("package.id", id.Id);
		act?.AddTag("package.version", id.Version.ToNormalizedString());

		using (logger.BeginScope(id))
		{
			LogMessages.ResolvingFrameworkCompatibleAssemblyPaths(
				logger,
				nuGetFramework.GetShortFolderName()
			);
			var libItems = (await packageReader.GetLibItemsAsync(cancellationToken)).ToArray();
			var nearest = frameworkReducer.GetNearest(nuGetFramework, libItems.Select(x => x.TargetFramework));

			var libPaths = libItems
				.Where(x => x.TargetFramework.Equals(nearest))
				.SelectMany(x => x.Items)
				.ToArray();

			act?.AddEvent(new ActivityEvent("GotLibraryPaths"));

			var frameworkItems = (await packageReader.GetFrameworkItemsAsync(cancellationToken)).ToArray();
			nearest = frameworkReducer.GetNearest(nuGetFramework, frameworkItems.Select(x => x.TargetFramework));

			var frameworkPaths = frameworkItems
				.Where(x => x.TargetFramework.Equals(nearest))
				.SelectMany(x => x.Items)
				.ToArray();

			act?.AddEvent(new ActivityEvent("GotFrameworkPaths"));

			return libPaths
				.Union(frameworkPaths)
				.Where(s => s.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
				.ToArray();
		}
	}

	private static partial class LogMessages
	{
		[LoggerMessage(LogLevel.Trace, "Found additional scan paths {AdditionalPaths}")]
		internal static partial void FoundAdditionalScanPaths(
			ILogger<PluginAssemblyScanner> logger,
			string[] additionalPaths
		);

		[LoggerMessage(LogLevel.Information, "Scanning for plugins in {PluginPath} implementing {PluginType}")]
		internal static partial void Scanning(
			ILogger<PluginAssemblyScanner> logger,
			string pluginPath,
			string pluginType
		);

		[LoggerMessage(LogLevel.Information, "Finished scanning for plugins in {PluginPath} implementing {PluginType}")]
		internal static partial void ScanComplete(
			ILogger<PluginAssemblyScanner> logger,
			string pluginPath,
			string pluginType
		);

		[LoggerMessage(LogLevel.Debug,
			"Found usable plugin implementation {PluginType}@{ContractType} at {AssemblyPath}")]
		internal static partial void FoundUsableImplementation(ILogger<PluginAssemblyScanner> logger, string pluginType,
			string contractType, string assemblyPath);

		[LoggerMessage(LogLevel.Debug, "Adding compatible assemblies from {PluginPath} to scan")]
		internal static partial void AddingPackagesToScan(ILogger<PluginAssemblyScanner> logger, string pluginPath);

		[LoggerMessage(LogLevel.Trace, "Constructing metadata type context for assembly ay {AssemblyFullPath}")]
		internal static partial void CreateTypeContext(
			ILogger<PluginAssemblyScanner> logger,
			string assemblyFullPath
		);

		[LoggerMessage(LogLevel.Trace, "Filtering types from assembly load context, sourced from {AssemblyFullPath}")]
		internal static partial void FilteringTypesFromContext(
			ILogger<PluginAssemblyScanner> logger,
			string assemblyFullPath
		);

		[LoggerMessage(LogLevel.Debug, "Loading implementations of {TypeName} from {AssemblyFullPath}")]
		internal static partial void GettingTypeImplementationsFromAssembly(
			ILogger<PluginAssemblyScanner> logger,
			string typeName,
			string assemblyFullPath
		);

		[LoggerMessage(LogLevel.Debug, "Resolving paths to compatible assemblies for {FrameworkName}")]
		internal static partial void ResolvingFrameworkCompatibleAssemblyPaths(
			ILogger<PluginAssemblyScanner> logger,
			string frameworkName
		);
	}
}
