using System.CommandLine;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Alexinea.Extensions.Configuration.Toml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Configuration.Memory;
using Spectre.Console;
using Spectre.Console.Json;
using Spectre.Console.Rendering;
using TradePath.Cli.Binders;

namespace TradePath.Cli.Commands;

internal static class ConfigurationCommands
{
	public static Command Build()
	{
		using var act = Telemetry.ActivitySource.StartActivityWithParent();
		var cmd = new Command(Resources.Commands.Configuration_Name, Resources.Commands.Configuration_Description)
		{
			BuildListCommand(),
			BuildDumpCommand()
		};
		cmd.AddGlobalOption(GlobalOptions.WorkingDirectory);
		return cmd;
	}

	private static Command BuildListCommand()
	{
		using var buildAct = Telemetry.ActivitySource.StartActivityWithParent();
		var cmd = new Command(
			Resources.Commands.Configuration_List_Name,
			Resources.Commands.Configuration_List_Description
		);

		cmd.SetHandler(HandleListCommand, new BindingContextService<IConfigurationRoot>(),
			new BindingContextService<IAnsiConsole>());

		return cmd;
	}

	private static Command BuildDumpCommand()
	{
		using var buildAct = Telemetry.ActivitySource.StartActivityWithParent();
		var cmd = new Command(
			Resources.Commands.Configuration_Dump_Name,
			Resources.Commands.Configuration_Dump_Description
		);

		cmd.SetHandler(HandleDumpCommand, new BindingContextService<IConfigurationRoot>(),
			new BindingContextService<IAnsiConsole>());

		return cmd;
	}

	private static void HandleDumpCommand(IConfigurationRoot config, IAnsiConsole console)
	{
		using var act = Telemetry.ActivitySource.StartActivityWithParent();

		var configValues = GenerateConfigValues(config);

		using var memoryStream = new MemoryStream();
		using var jsonWriter = new Utf8JsonWriter(memoryStream);

		jsonWriter.WriteStartObject();

		foreach (var keyValuePair in configValues)
		{
			jsonWriter.WritePropertyName(keyValuePair.Key);
			jsonWriter.WriteStartObject();
			jsonWriter.WriteString("value", keyValuePair.Value.Value);
			jsonWriter.WriteString("source", keyValuePair.Value.Source);
			jsonWriter.WriteEndObject();
			jsonWriter.Flush();
		}

		jsonWriter.WriteEndObject();
		jsonWriter.Flush();

		var jsonString = Encoding.UTF8.GetString(memoryStream.ToArray());

		console.Write(new JsonText(jsonString));
	}

	private record ConfigValue(
		string Source,
		string Value
	);

	private static Dictionary<string, ConfigValue> GenerateConfigValues(IConfigurationRoot config)
	{
		using var act = Telemetry.ActivitySource.StartActivityWithParent();

		return config.AsEnumerable()
			.Select(kvp => new
			{
				kvp.Key,
				Value = kvp.Value ?? "''",
				Source = DetermineSource(kvp.Key, kvp.Value, config.Providers)
			})
			.Where(x => x.Source != null)
			.ToDictionary(x => x.Key, x => new ConfigValue(x.Source!, x.Value), StringComparer.OrdinalIgnoreCase);

		string? DetermineSource(string key, string? value, IEnumerable<IConfigurationProvider> providers)
		{
			if (value == null) return null;

			foreach (var provider in providers.Reverse())
			{
				if (provider.TryGet(key, out _))
				{
					return provider switch
					{
#if WINDOWS
	                WindowsConfigurationBinder.WindowsConfigurationProvider => Resources.Commands.Configuration_Sources_AppSettings,
#endif
						TomlConfigurationProvider toml => $"file://{toml.Source.Path!}",
						EnvironmentVariablesConfigurationProvider env => GetEnvironmentSource(env, key),
						MemoryConfigurationProvider => Resources.Commands.Configuration_Sources_BuiltIn,
						_ => Resources.Commands.Configuration_Sources_Unknown
					};
				}
			}

			return null;

			string? GetEnvironmentSource(EnvironmentVariablesConfigurationProvider env, string k)
			{
				var prefix = env.GetType()
					.GetField("_prefix", BindingFlags.NonPublic | BindingFlags.Instance)
					?.GetValue(env) as string;

				return string.IsNullOrWhiteSpace(prefix) ? null : $"env://{prefix}{k.Replace(":", "__")}";
			}
		}
	}

	private static void HandleListCommand(IConfigurationRoot config, IAnsiConsole console)
	{
		using var act = Telemetry.ActivitySource.StartActivityWithParent();

		var configValues = GenerateConfigValues(config);

		if (!console.Profile.Out.IsTerminal)
		{
			act?.AddBaggage("app.console.output.terminal", bool.FalseString);
			var strBuilder = new StringBuilder();
			foreach (var (key, value) in configValues)
			{
				strBuilder
					.Append(key).Append(',').Append(value.Value).Append(',').AppendLine(value.Source);
			}

			console.WriteLine(strBuilder.ToString());

			return;
		}

		var table = new Table()
			.Border(TableBorder.Minimal)
			.AddColumn(Resources.Commands.Configuration_List_Column_Key)
			.AddColumn(Resources.Commands.Configuration_List_Column_Value)
			.AddColumn(Resources.Commands.Configuration_List_Column_Source);

		foreach (var (key, value) in configValues)
		{
			IRenderable source;
			if (value.Source.StartsWith("file://"))
			{
				source = new TextPath(value.Source);
			}
			else if (value.Source.StartsWith("env://"))
			{
				source = new Text(value.Source, new Style(Color.Blue));
			}
			else
			{
				source = new Text(value.Source);
			}


			table.AddRow(
				new Text(key),
				new Text(value.Value),
				source
			);
		}

		console.Write(table);
	}
}
