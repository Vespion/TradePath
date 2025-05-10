using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using NuGet.Common;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

#pragma warning disable CA2254

namespace TradePath.Plugins.HostHelpers.Nuget;

[SuppressMessage("ReSharper", "TemplateIsNotCompileTimeConstantProblem")]
internal class NugetLoggingAdaptor(ILogger logger) : NuGet.Common.ILogger
{
	/// <inheritdoc />
	public void LogDebug(string data)
	{
		logger.LogDebug(data);
	}

	/// <inheritdoc />
	public void LogVerbose(string data)
	{
		logger.LogTrace(data);
	}

	/// <inheritdoc />
	public void LogInformation(string data)
	{
		logger.LogInformation(data);
	}

	/// <inheritdoc />
	public void LogMinimal(string data)
	{
		logger.LogInformation(data);
	}

	/// <inheritdoc />
	public void LogWarning(string data)
	{
		logger.LogWarning(data);
	}

	/// <inheritdoc />
	public void LogError(string data)
	{
		logger.LogError(data);
	}

	/// <inheritdoc />
	public void LogInformationSummary(string data)
	{
		logger.LogInformation(data);
	}

	/// <inheritdoc />
	public void Log(NuGet.Common.LogLevel level, string data)
	{
		switch (level)
		{
			case NuGet.Common.LogLevel.Debug:
				LogDebug(data);
				break;
			case NuGet.Common.LogLevel.Verbose:
				LogVerbose(data);
				break;
			case NuGet.Common.LogLevel.Information:
				LogInformation(data);
				break;
			case NuGet.Common.LogLevel.Minimal:
				LogMinimal(data);
				break;
			case NuGet.Common.LogLevel.Warning:
				LogWarning(data);
				break;
			case NuGet.Common.LogLevel.Error:
				LogError(data);
				break;
			default:
				Debug.WriteLine($"Unknown log level: {level} - {data}");
				break;
		}
	}

	/// <inheritdoc />
	public Task LogAsync(NuGet.Common.LogLevel level, string data)
	{
		Log(level, data);
		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public void Log(ILogMessage message)
	{
		var logLevel = message.Level switch
		{
			NuGet.Common.LogLevel.Debug => LogLevel.Debug,
			NuGet.Common.LogLevel.Verbose => LogLevel.Trace,
			NuGet.Common.LogLevel.Information => LogLevel.Information,
			NuGet.Common.LogLevel.Minimal => LogLevel.Information,
			NuGet.Common.LogLevel.Warning => LogLevel.Warning,
			NuGet.Common.LogLevel.Error => LogLevel.Error,
			_ => LogLevel.None
		};

		if (logLevel == LogLevel.None)
		{
			Debug.WriteLine($"Unknown log level: {message.Level} - {message.Message}");
		}

		logger.Log(logLevel, new EventId((int)message.Code, message.Code.ToString()), message.Message);
	}

	/// <inheritdoc />
	public Task LogAsync(ILogMessage message)
	{
		Log(message);
		return Task.CompletedTask;
	}
}
