using System.Diagnostics;
using System.Reflection;

namespace TradePath.Plugins.HostHelpers;

internal static class Telemetry
{
	public static AssemblyName AssemblyName => typeof(Telemetry).Assembly.GetName();
	
	public static Version AssemblyVersion { get; } = AssemblyName.Version ?? new Version(0, 0, 0, 0);
	
	public static string InformationalVersion { get; } = typeof(Telemetry).Assembly
		.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
		.OfType<AssemblyInformationalVersionAttribute>()
		.FirstOrDefault()?.InformationalVersion ?? AssemblyVersion.ToString(3);
	
	public static ActivitySource ActivitySource { get; } = new(AssemblyName.Name ?? AssemblyName.FullName, InformationalVersion);
}
