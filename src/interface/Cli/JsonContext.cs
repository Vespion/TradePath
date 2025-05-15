using System.Text.Json.Serialization;
using TradePath.Plugins.HostHelpers;

namespace TradePath.Cli;

[JsonSerializable(typeof(ConfigurationModel))]
internal partial class JsonContext: JsonSerializerContext;
