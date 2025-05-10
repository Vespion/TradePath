namespace TradePath.Plugins.Contracts;

/// <summary>
///     A progress object is used to report the progress of a plugin task back to the host.
/// </summary>
/// <remarks>
///     <p>The collections should be used to report updates, not to contain the entire state of the plugin.</p>
///     <p>
///         For example, if a task is completed in this update, the next update should not include it in the
///         <see cref="CompletedTasks" /> collection.
///     </p>
/// </remarks>
/// <param name="PendingTasks">A collection of tasks that are pending but have not started</param>
/// <param name="RunningTasks">A collection of tasks that are running</param>
/// <param name="CompletedTasks">A collection of tasks that have completed and should be removed form the host UI</param>
/// <param name="FailedTasks">A collection of tasks that have failed</param>
public readonly record struct PluginProgress(
	ICollection<PluginTask> PendingTasks,
	ICollection<PluginTask> RunningTasks,
	ICollection<PluginTask> CompletedTasks,
	ICollection<PluginTask> FailedTasks
);

/// <summary>
///     A plugin task is used to report the progress of a plugin task back to the host.
/// </summary>
/// <param name="Description">A localised description</param>
/// <param name="CurrentStep">The current step count</param>
/// <param name="MaxSteps">The number of steps required for completion</param>
public readonly record struct PluginTask(
	string Description,
	uint? CurrentStep,
	uint? MaxSteps
);
