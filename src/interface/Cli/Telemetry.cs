using System.Diagnostics;
using System.Reflection;

namespace TradePath.Cli;

internal static class Telemetry
{
	public static Version AssemblyVersion { get; } = typeof(Telemetry).Assembly.GetName().Version ?? new Version(0, 0, 0, 0);
	
	public static string InformationalVersion { get; } = typeof(Telemetry).Assembly
		.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
		.OfType<AssemblyInformationalVersionAttribute>()
		.FirstOrDefault()?.InformationalVersion ?? AssemblyVersion.ToString(3);
	
	public static ActivitySource ActivitySource { get; } = new("TradePath.Cli", InformationalVersion);
}
