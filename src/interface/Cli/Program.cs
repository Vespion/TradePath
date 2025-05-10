// See https://aka.ms/new-console-template for more information

using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Spectre.Console;
using TradePath.Cli;
using TradePath.Cli.Commands;

var resourceBuilder = ResourceBuilder
	.CreateDefault()
	.AddTelemetrySdk()
	.AddEnvironmentVariableDetector()
	.AddHostDetector()
	.AddOperatingSystemDetector()
	.AddProcessRuntimeDetector()
	.AddService(
		Telemetry.ActivitySource.Name,
		"TradePath",
		Telemetry.ActivitySource.Version
	);

var tracerProvider = Sdk.CreateTracerProviderBuilder()
	.AddSource(Telemetry.ActivitySource.Name, "TradePath.*")
	.AddEntityFrameworkCoreInstrumentation()
	.AddHttpClientInstrumentation()
	.SetResourceBuilder(resourceBuilder)
	.AddOtlpExporter()
	.Build();

var meterProvider = Sdk.CreateMeterProviderBuilder()
	.AddMeter(Telemetry.ActivitySource.Name, "TradePath.*")
	.AddProcessInstrumentation()
	.AddRuntimeInstrumentation()
	.AddHttpClientInstrumentation()
	.SetResourceBuilder(resourceBuilder)
	.AddOtlpExporter()
	.Build();

var loggerFactory = LoggerFactory.Create(lb =>
{
	lb.SetMinimumLevel(LogLevel.Trace);

	lb.AddOpenTelemetry(logging =>
	{
		logging.SetResourceBuilder(resourceBuilder);
		logging.AddOtlpExporter();
	});

	lb.AddDebug();
});

var logger = loggerFactory.CreateLogger<Program>();

LogMessages.TelemetryActive(logger);

try
{
	using var act = Telemetry.ActivitySource.StartActivityWithParent();

	LogMessages.ConstructingCommandParser(logger);
	var rootCommand = new RootCommand("Command line interface for TradePath")
	{
		Name = "tradepath"
	};

	rootCommand.AddCommand(DatabaseCommands.Build());
	rootCommand.AddCommand(PluginCommands.Build());

	act?.AddEvent(new ActivityEvent("ConfiguredCommandTree"));

	var builder = new CommandLineBuilder(rootCommand);
	LogMessages.CommandTreeConstructed(logger);
	
	builder.UseDefaults();

	builder.AddMiddleware(context =>
	{
		// ReSharper disable once AccessToDisposedClosure
		context.BindingContext.AddService(_ => loggerFactory);
		context.BindingContext.AddService(typeof(IMeterFactory), _ => new Telemetry.MeterFactory());
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
	}, MiddlewareOrder.Configuration);

	act?.AddEvent(new ActivityEvent("ConfiguredPipeline"));

	var parser = builder.Build();
	act?.AddEvent(new ActivityEvent("BuiltParser"));
	LogMessages.CommandParserConstructed(logger);

	// ReSharper disable once ExplicitCallerInfoArgument
	// ReSharper disable once ConvertToUsingDeclaration
	using (var cmdAct = Telemetry.ActivitySource.StartActivityWithParent("HandleCommandExecution", ActivityKind.Server))
	{
		LogMessages.CommandExecutionStarted(logger, args);
		
		try
		{
			var result = await parser.InvokeAsync(args);
			cmdAct?.AddEvent(new ActivityEvent("InvocationComplete", default, new ActivityTagsCollection
				{
					{ "exit_code", result }
				}
			));
			LogMessages.CommandExecutionComplete(logger, result);
			cmdAct?.SetStatus(ActivityStatusCode.Ok, result.ToString());
			return result;
		}
		catch (Exception ex)
		{
			LogMessages.CommandExecutionFailed(logger, ex);
			cmdAct?.AddException(ex).SetStatus(ActivityStatusCode.Error);
			return -1;
		}
	}
}
finally
{
	tracerProvider.ForceFlush();
	meterProvider.ForceFlush();

	tracerProvider.Dispose();
	meterProvider.Dispose();
	loggerFactory.Dispose();
}

static partial class LogMessages
{
	[LoggerMessage(Level = LogLevel.Critical, Message = "An error occurred during command execution")]
	internal static partial void CommandExecutionFailed(ILogger<Program> logger, Exception ex);
	
	[LoggerMessage(Level = LogLevel.Information, Message = "Command execution complete, exited with code {ExitCode}")]
	internal static partial void CommandExecutionComplete(ILogger<Program> logger, int exitCode);
	
	[LoggerMessage(Level = LogLevel.Information, Message = "Command execution started with args {Args}")]
	internal static partial void CommandExecutionStarted(ILogger<Program> logger, string[] args);
	
	[LoggerMessage(Level = LogLevel.Debug, Message = "Telemetry active")]
	internal static partial void TelemetryActive(ILogger<Program> logger);
	
	[LoggerMessage(Level = LogLevel.Trace, Message = "Command tree constructed")]
	internal static partial void CommandTreeConstructed(ILogger<Program> logger);
	
	[LoggerMessage(Level = LogLevel.Debug, Message = "Command parser constructed")]
	internal static partial void CommandParserConstructed(ILogger<Program> logger);

	[LoggerMessage(Level = LogLevel.Debug, Message = "Constructing command parser")]
	internal static partial void ConstructingCommandParser(ILogger<Program> logger);
}
