namespace TradePath.Plugins.HostHelpers.Models;

public record struct PluginInstallationProgress(
	IDictionary<string, string>? NewResolutionTasks = null,
	ICollection<string>? CompletedResolutionTasks = null,
	bool RunningSimplification = false,
	IDictionary<string, int>? InstallationTasks = null
)
{
	public enum PackageInstallationStage
	{
		Download,
		Reading,
		Extraction,
	}
}
