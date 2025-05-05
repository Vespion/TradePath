using Prise.Plugin;
using TradePath.Models;
using TradePath.Plugins.Contracts;
using StarSystem = TradePath.Models.StarSystem;
using Station = TradePath.Models.Station;

namespace TradePath.Plugins.Edsm;

[Plugin(PluginType = typeof(ISystemNavigationProvider))]
public class SystemNavigationProvider : ISystemNavigationProvider
{
	/// <inheritdoc />
	public IAsyncEnumerable<StarSystem> GetSystemsData(IProgress<PluginProgress> progress, TimeSpan ageHint = default)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	public IAsyncEnumerable<Station> GetStationData(IProgress<PluginProgress> progress, TimeSpan ageHint = default)
	{
		throw new NotImplementedException();
	}
}
