namespace TradePath.Plugins.HostHelpers.Progress;

public readonly record struct GetInstalledPluginsProgress(
	GetInstalledPluginsProgress.Stage CurrentStage,
	uint? Step,
	uint? TotalSteps
)
{
	public enum Stage
	{
		ScanningInstallation,
		ReadingPackageManifests
	}
}
