using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.Packaging.Signing;
using NuGet.ProjectManagement;
using ExecutionContext = NuGet.ProjectManagement.ExecutionContext;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace TradePath.Plugins.HostHelpers.Nuget.Project;

public class PluginInstallationContext(ILogger<PluginInstallationContext> logger): INuGetProjectContext
{
	private readonly NugetLoggingAdaptor _loggingAdaptor = new(logger);
	
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
		
		logger.Log(logLevel, message, args);
	}

	/// <inheritdoc />
	public void Log(ILogMessage message)
	{
		_loggingAdaptor.Log(message);
	}

	/// <inheritdoc />
	public void ReportError(string message)
	{
		logger.LogError(message);
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

	public PackageExtractionContext PackageExtractionContext { get; set; }
	public ISourceControlManagerProvider SourceControlManagerProvider { get; }
	public ExecutionContext ExecutionContext { get; }
	public XDocument OriginalPackagesConfig { get; set; }
	public NuGetActionType ActionType { get; set; }
	public Guid OperationId { get; set; }
}
