using StarSystem = TradePath.Models.StarSystem;
using Station = TradePath.Models.Station;

namespace TradePath.Plugins.Contracts;

/// <summary>
///     This interface is used to provide star system and station data to the host application.
/// </summary>
public interface ISystemNavigationProvider
{
	/// <summary>
	///     Get the data for the star systems that require updating.
	/// </summary>
	/// <param name="progress">Progress object to feedback current status to the host, the plugin should handle localisation</param>
	/// <param name="ageHint">
	///     A hint from the host that indicates how long ago the plugin should search for data. The default
	///     value indicates the plugin should return *all* available data
	/// </param>
	/// <returns></returns>
	IAsyncEnumerable<StarSystem> GetSystemsData(IProgress<PluginProgress> progress, TimeSpan ageHint = default);

	/// <summary>
	///     Get the basic data for a station in a star system.
	/// </summary>
	/// <param name="progress">Progress object to feedback current status to the host, the plugin should handle localisation</param>
	/// <param name="ageHint">
	///     A hint from the host that indicates how long ago the plugin should search for data. The default
	///     value indicates the plugin should return *all* available data
	/// </param>
	/// <returns>A populated station object, note that sub objects such as markets are not required or expected to be present.</returns>
	IAsyncEnumerable<Station> GetStationData(IProgress<PluginProgress> progress, TimeSpan ageHint = default);
}
