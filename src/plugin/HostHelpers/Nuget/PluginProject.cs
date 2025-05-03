using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.ProjectManagement;
using NuGet.ProjectModel;

namespace TradePath.Plugins.HostHelpers.Nuget;

public class PluginProject: FolderNuGetProject
{
	/// <inheritdoc />
	public PluginProject(string root) : base(root, new PackagePathResolver(root), NuGetFramework.ParseFolder("net9.0"))
	{
		ProjectStyle = ProjectStyle.Standalone;
	}
}
