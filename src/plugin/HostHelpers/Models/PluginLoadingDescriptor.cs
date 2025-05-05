using Prise;

namespace TradePath.Plugins.HostHelpers.Models;

public readonly record struct PluginLoadingDescriptor(PluginDescriptor Descriptor, AssemblyScanResult scanResult);
