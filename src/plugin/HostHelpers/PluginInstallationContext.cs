using Microsoft.Extensions.Logging;
using NuGet.Packaging.Core;
using TradePath.Plugins.HostHelpers.Progress;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace TradePath.Plugins.HostHelpers;

public class PluginInstallationContext
{
	private readonly ILogger<PluginInstallationContext> _logger;
	private static readonly PackageType TradePathPlugin = new("TradePath.Plugin", version: new Version(1, 0));
	private static readonly PackageType SystemNavigationProvider = new("TradePath.Plugin.SystemNavigationProvider", version: new Version(1, 0));
	
	public PluginInstallationContext(string root, ILogger<PluginInstallationContext> logger)
	{
		_logger = logger;
	}

	public async Task InstallPlugin(PackageIdentity pluginId, CancellationToken ct = default)
	{
		
	}

	public async Task<IReadOnlyCollection<PluginSpec>> GetInstalledPlugins(
		IProgress<GetInstalledPluginsProgress>? progress = null, CancellationToken ct = default)
	{
		return new List<PluginSpec>();
	}
	
	// public async Task<IReadOnlyCollection<PluginSpec>> GetInstalledPlugins(IProgress<GetInstalledPluginsProgress>? progress = null, CancellationToken ct = default)
	// {
	// 	progress?.Report(new GetInstalledPluginsProgress(GetInstalledPluginsProgress.Stage.ScanningInstallation, null, null));
	// 	var packages = (await Project.GetInstalledPackagesAsync(ct)).ToArray();
	// 	
	// 	var maxPackages = (uint)packages.Length;
	// 	var list = new List<PluginSpec>();
	// 	for (uint i = 0; i < packages.Length; i++)
	// 	{
	// 		var package = packages[i];
	// 		progress?.Report(new GetInstalledPluginsProgress(GetInstalledPluginsProgress.Stage.ReadingPackageManifests, i, maxPackages));
	// 		
	// 		using var reader = new PackageFolderReader(Project.GetInstalledPath(package.PackageIdentity));
	//
	// 		var types = reader.NuspecReader.GetPackageTypes();
	//
	// 		if (!types.Contains(TradePathPlugin))
	// 		{
	// 			continue;
	// 		}
	//
	// 		var licenseMetadata = reader.NuspecReader.GetLicenseMetadata();
	//
	// 		var licenseString = licenseMetadata.LicenseExpression switch
	// 		{
	// 			NuGetLicense license => license.ToString(),
	// 			_ => null
	// 		};
	//
	// 		if (licenseMetadata.LicenseExpression.Type == LicenseExpressionType.License)
	// 		{
	// 		}
	//
	// 		list.Add(new PluginSpec(
	// 				reader.NuspecReader.GetTitle(),
	// 				reader.NuspecReader.GetAuthors(),
	// 				licenseString,
	// 				types.Contains(SystemNavigationProvider)
	// 			)
	// 		);
	// 	}
	//
	// 	return list.AsReadOnly();
	// }
// 	
// 	/// <inheritdoc />
// 	void INuGetProjectContext.Log(MessageLevel level, string message, params object[] args)
// 	{
// 		var logLevel = level switch
// 		{
// 			MessageLevel.Info => LogLevel.Information,
// 			MessageLevel.Warning => LogLevel.Warning,
// 			MessageLevel.Debug => LogLevel.Debug,
// 			MessageLevel.Error => LogLevel.Error,
// 			_ => LogLevel.None
// 		};
// 		
// #pragma warning disable CA2254
// 		_logger.Log(logLevel, message, args);
// #pragma warning restore CA2254
// 	}
//
// 	/// <inheritdoc />
// 	void INuGetProjectContext.Log(ILogMessage message)
// 	{
// 		var logLevel = message.Level switch
// 		{
// 			NuGet.Common.LogLevel.Debug => LogLevel.Debug,
// 			NuGet.Common.LogLevel.Verbose => LogLevel.Trace,
// 			NuGet.Common.LogLevel.Information => LogLevel.Information,
// 			NuGet.Common.LogLevel.Minimal => LogLevel.Information,
// 			NuGet.Common.LogLevel.Warning => LogLevel.Warning,
// 			NuGet.Common.LogLevel.Error => LogLevel.Error,
// 			_ => LogLevel.None
// 		};
// 		
// 		_logger.Log(logLevel, new EventId((int)message.Code, message.Code.ToString()), message.Message);
// 	}
}
