using Prise;

namespace TradePath.Plugins.HostHelpers.Models;

/// <summary>
///     Information the result of a plugin scan.
/// </summary>
/// <param name="Descriptor">The plugin that implements the scanned for type</param>
/// <param name="ScanResult">
///     Primarily intended for internal use, contains information for the plugin loader such as
///     assembly location.
/// </param>
public readonly record struct PluginLoadingDescriptor(PluginDescriptor Descriptor, AssemblyScanResult ScanResult);
