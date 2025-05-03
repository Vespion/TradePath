using System.CommandLine;
using Microsoft.Extensions.Logging;

namespace TradePath.Cli.Commands;

internal static class GlobalOptions
{
	public static Option<DirectoryInfo> WorkingDirectory { get; } = new(
		Resources.Commands.Global_Directory_Name.Split('|'),
		() => new DirectoryInfo(Environment.CurrentDirectory),
		Resources.Commands.Global_Directory_Description
	)
	{
		IsRequired = false,
		Arity = ArgumentArity.ZeroOrOne
	};
	
	public static Option<LogLevel> Verbosity { get; } = new(
		Resources.Commands.Global_Verbosity_Name.Split('|'),
		() => LogLevel.Information,
		Resources.Commands.Global_Verbosity_Description
	)
	{
		IsRequired = false,
		Arity = ArgumentArity.ZeroOrOne
	};
}
