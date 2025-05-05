using System.Reflection;
using NuGet.Frameworks;
using NuGet.Packaging;
using Prise;
using Prise.AssemblyScanning;
using Prise.Plugin;

namespace TradePath.Plugins.HostHelpers.Prise;

public sealed class PluginAssemblyScanner : DefaultAssemblyScanner
{
	/// <inheritdoc />
	public PluginAssemblyScanner(Func<string, IMetadataLoadContext> metadataLoadContextFactory,
		Func<IDirectoryTraverser> directoryTraverser) : base(metadataLoadContextFactory, directoryTraverser)
	{
	}

	/// <inheritdoc />
	public override async Task<IEnumerable<AssemblyScanResult>> Scan(IAssemblyScannerOptions options)
	{
		var nuGetFramework = NuGetFramework.AnyFramework;
		var frameworkReducer = new FrameworkReducer();

		var scanPaths = new List<string>();

		foreach (var pluginDirectory in directoryTraverser.TraverseDirectories(options.StartingPath))
		{
			var packageReader = new PackageFolderReader(pluginDirectory);

			scanPaths.AddRange(
				(await GetCompatibleFrameworkPaths(frameworkReducer, nuGetFramework, packageReader))
				.Select(s => Path.Combine(pluginDirectory, s))
			);
		}

		var scanResults = new List<AssemblyScanResult>(scanPaths.Count);

		foreach (var scanPath in scanPaths)
		{
			foreach (var implementationType in GetImplementationsOfTypeFromAssembly(options.PluginType, scanPath))
			{
				if (implementationType != null)
				{
					scanResults.Add(new AssemblyScanResult()
					{
						ContractType = options.PluginType,
						AssemblyName = Path.GetFileName(scanPath),
						AssemblyPath = Path.GetDirectoryName(scanPath),
						PluginType = implementationType
					});
				}
			}
		}

		return scanResults;
	}

	private IEnumerable<Type?> GetImplementationsOfTypeFromAssembly(Type type, string assemblyFullPath)
	{
		var metadataLoadContext = metadataLoadContextFactory(assemblyFullPath);
		var assemblyShim = metadataLoadContext.LoadFromAssemblyName(Path.GetFileNameWithoutExtension(assemblyFullPath));
		disposables.Add(metadataLoadContext);
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
		var libItems = (await packageReader.GetLibItemsAsync(cancellationToken)).ToArray();
		var nearest = frameworkReducer.GetNearest(nuGetFramework, libItems.Select(x => x.TargetFramework));

		var libPaths = libItems
			.Where(x => x.TargetFramework.Equals(nearest))
			.SelectMany(x => x.Items)
			.ToArray();

		var frameworkItems = (await packageReader.GetFrameworkItemsAsync(cancellationToken)).ToArray();
		nearest = frameworkReducer.GetNearest(nuGetFramework, frameworkItems.Select(x => x.TargetFramework));

		var frameworkPaths = frameworkItems
			.Where(x => x.TargetFramework.Equals(nearest))
			.SelectMany(x => x.Items)
			.ToArray();

		return libPaths
			.Union(frameworkPaths)
			.Where(s => s.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
			.ToArray();
	}
}
