using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace TradePath.Cli;

internal static class Telemetry
{
	internal static string Name => AssemblyName.Name ?? AssemblyName.FullName;
	
	private static AssemblyName AssemblyName => typeof(Telemetry).Assembly.GetName();

	private static Version AssemblyVersion { get; } = AssemblyName.Version ?? new Version(0, 0, 0, 0);

	private static string InformationalVersion { get; } = typeof(Telemetry).Assembly
		.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
		.OfType<AssemblyInformationalVersionAttribute>()
		.FirstOrDefault()?.InformationalVersion ?? AssemblyVersion.ToString(3);

	public static ActivitySource ActivitySource { get; } =
		new(Name, InformationalVersion);
	
	public static Activity? StartActivityWithParent(this ActivitySource activitySource, [CallerMemberName] string name = "", ActivityKind kind = ActivityKind.Internal)
	{
		var currentActivity = Activity.Current;
		if (currentActivity == null)
		{
			return activitySource.StartActivity(name, kind);
		}
		
		var newActivity = activitySource.StartActivity(name, kind);
		if (currentActivity.Id != null) newActivity?.SetParentId(currentActivity.Id);

		return newActivity;
	}


	public class MeterFactory : IMeterFactory
	{
		/// <inheritdoc />
		public void Dispose()
		{
		}

		/// <inheritdoc />
		public Meter Create(MeterOptions options)
		{
			return new Meter(options);
		}
	}
}
