namespace TradePath.Plugins.HostHelpers.Models;

/// <summary>
///     Used to report the progress of a plugin installation.
/// </summary>
public record struct PluginInstallationProgress(
	IDictionary<string, string>? NewResolutionTasks = null,
	ICollection<string>? CompletedResolutionTasks = null,
	bool RunningSimplification = false,
	IDictionary<string, int>? InstallationTasks = null
);
