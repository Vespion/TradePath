using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.PackageManagement;
using NuGet.Packaging;
using NuGet.Packaging.Signing;
using NuGet.ProjectManagement;
using ExecutionContext = NuGet.ProjectManagement.ExecutionContext;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
#pragma warning disable CA2254

namespace TradePath.Plugins.HostHelpers.Nuget;

[SuppressMessage("ReSharper", "TemplateIsNotCompileTimeConstantProblem")]
public class PluginProjectContext : INuGetProjectContext
{
	private readonly NugetLoggingAdaptor _loggingAdaptor;
	private readonly ILogger _logger;

	public PluginProjectContext(ILogger logger, ISettings settings)
	{
		_logger = logger;
		_loggingAdaptor = new NugetLoggingAdaptor(logger);
		
		PackageExtractionContext = new PackageExtractionContext(
			PackageSaveMode.Nuspec | PackageSaveMode.Files,
			XmlDocFileSaveMode.Skip,
			ClientPolicyContext.GetClientPolicy(settings, _loggingAdaptor),
			_loggingAdaptor
		);
	}

	/// <inheritdoc />
	public void Log(MessageLevel level, string message, params object[] args)
	{
		var logLevel = level switch
		{
			MessageLevel.Info => LogLevel.Information,
			MessageLevel.Warning => LogLevel.Warning,
			MessageLevel.Debug => LogLevel.Debug,
			MessageLevel.Error => LogLevel.Error,
			_ => LogLevel.None
		};
		
		_logger.Log(logLevel, message, args);
	}

	/// <inheritdoc />
	public void Log(ILogMessage message)
	{
		_loggingAdaptor.Log(message);
	}

	/// <inheritdoc />
	public void ReportError(string message)
	{
		_logger.LogError(message);
	}

	/// <inheritdoc />
	public void ReportError(ILogMessage message)
	{
		_loggingAdaptor.Log(message);
	}

	/// <inheritdoc />
	public FileConflictAction ResolveFileConflict(string message)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	public PackageExtractionContext PackageExtractionContext { get; set; }

	/// <inheritdoc />
	public ISourceControlManagerProvider? SourceControlManagerProvider => null;

	/// <inheritdoc />
	public ExecutionContext? ExecutionContext => null;

	/// <inheritdoc />
	public XDocument OriginalPackagesConfig { get; set; }

	/// <inheritdoc />
	public NuGetActionType ActionType { get; set; }

	/// <inheritdoc />
	public Guid OperationId { get; set; }
}
