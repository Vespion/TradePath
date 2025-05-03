// See https://aka.ms/new-console-template for more information

using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Spectre.Console;
using TradePath.Cli;
using TradePath.Cli.Commands;

using var act = Telemetry.ActivitySource.StartActivity();

var rootCommand = new RootCommand("Command line interface for TradePath")
{
	Name = "tradepath"
};

rootCommand.AddCommand(DatabaseCommands.Build());
rootCommand.AddCommand(PluginCommands.Build());

var builder = new CommandLineBuilder(rootCommand);

builder.UseDefaults();
builder.AddMiddleware(context =>
{
	context.BindingContext.AddService(typeof(IAnsiConsole), sp =>
	{
		var console = sp.GetRequiredService<IConsole>();
		return AnsiConsole.Create(new AnsiConsoleSettings
		{
			Ansi = console.IsOutputRedirected ? AnsiSupport.No : AnsiSupport.Detect,
			Out = new AnsiConsoleOutput(console.Out.CreateTextWriter()),
			ColorSystem = ColorSystemSupport.Detect,
			Interactive = console.IsInputRedirected ? InteractionSupport.No : InteractionSupport.Detect,
			Enrichment = new ProfileEnrichment
			{
				UseDefaultEnrichers = true
			}
		});
	});
	
	context.BindingContext.AddService(_ => LoggerFactory.Create(lb =>
	{
		lb.SetMinimumLevel(LogLevel.Trace);

		lb.AddDebug();
	}));
	
}, MiddlewareOrder.Configuration);

act?.AddEvent(new ActivityEvent("Configured builder"));
var parser = builder.Build();
act?.AddEvent(new ActivityEvent("Built parser"));

var result = await parser.InvokeAsync(args);
act?.AddEvent(new ActivityEvent("Invocation complete", default, new ActivityTagsCollection
	{
		{"ExitCode", result}
	}
));

return result;
