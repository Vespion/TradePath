using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace TradePath.Plugins.HostHelpers;

internal static class Telemetry
{
	private static AssemblyName AssemblyName => typeof(Telemetry).Assembly.GetName();

	private static Version AssemblyVersion { get; } = AssemblyName.Version ?? new Version(0, 0, 0, 0);

	public static string InformationalVersion { get; } = typeof(Telemetry).Assembly
		.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
		.OfType<AssemblyInformationalVersionAttribute>()
		.FirstOrDefault()?.InformationalVersion ?? AssemblyVersion.ToString(3);

	public static ActivitySource ActivitySource { get; } =
		new(AssemblyName.Name ?? AssemblyName.FullName, InformationalVersion);

	public static Activity? StartActivityWithParent(this ActivitySource activitySource, [CallerMemberName] string name = "")
	{
		var currentActivity = Activity.Current;
		if (currentActivity == null)
		{
			return activitySource.StartActivity(name);
		}
		
		var newActivity = activitySource.StartActivity(name);
		if (currentActivity.Id != null) newActivity?.SetParentId(currentActivity.Id);

		return newActivity;
	}
}
