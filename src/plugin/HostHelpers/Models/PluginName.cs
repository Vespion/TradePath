using Vogen;

namespace TradePath.Plugins.HostHelpers.Models;

/// <summary>
///     Represents the ID of a plugin. Name is used to mean the NuGet package ID, not the display name.
/// </summary>
[ValueObject<string>]
public readonly partial record struct PluginName;
