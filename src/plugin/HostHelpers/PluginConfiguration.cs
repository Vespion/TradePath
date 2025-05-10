namespace TradePath.Plugins.HostHelpers;

/// <summary>
///     Represents the configuration for the plugin manager.
/// </summary>
/// <param name="PluginFolder">The folder to store plugin assemblies and support files.</param>
public record PluginConfiguration(string PluginFolder);
