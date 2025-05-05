using System.Text.Json.Serialization;

namespace TradePath.Plugins.HostHelpers.Models;

[JsonSerializable(typeof(PluginDescriptor))]
internal partial class JsonContext : JsonSerializerContext;
