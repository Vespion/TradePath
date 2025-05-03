namespace TradePath.Plugins.Contracts;

public readonly record struct PluginProgress(
	ICollection<PluginTask> PendingTasks,
	ICollection<PluginTask> RunningTasks,
	ICollection<PluginTask> CompletedTasks,
	ICollection<PluginTask> FailedTasks
);

public readonly record struct PluginTask(
	string Description,
	uint? CurrentStep,
	uint? MaxSteps
);
