using Microsoft.Extensions.DependencyModel;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.ProjectModel;
using NuGet.Versioning;

namespace TradePath.Plugins.HostHelpers.Nuget.Project;

public class PluginInstallationProject : FolderNuGetProject
{
	public PluginInstallationProject(string root) : base(root,
		new PackagePathResolver(root),
		NuGetFramework.ParseFolder("net9.0"))
	{
		ProjectStyle = ProjectStyle.Standalone;
	}

	// public override async Task<IEnumerable<PackageReference>> GetInstalledPackagesAsync(CancellationToken token)
	// {
	// 	return DependencyContext.Default!.RuntimeLibraries
	// 		.Where(x => x.Name == "TradePath.Plugins.Contracts")
	// 		.Select(l => new PackageReference(new PackageIdentity(l.Name, NuGetVersion.Parse(l.Version)), NuGetFramework.ParseFolder(DependencyContext.Default.Target.Framework)))
	// 		.Concat(await base.GetInstalledPackagesAsync(token));
	// }
}
