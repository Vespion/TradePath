using ExecutionContext = NuGet.ProjectManagement.ExecutionContext;

namespace TradePath.Plugins.HostHelpers.Nuget;

public class PluginExecutionContext: ExecutionContext
{
	/// <inheritdoc />
	public override Task OpenFile(string fullPath)
	{
		throw new NotImplementedException();
	}
}
