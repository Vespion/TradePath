#if WINDOWS
using Windows.Storage;
using Microsoft.Extensions.Configuration;

namespace TradePath.Cli;


internal static class WindowsConfigurationBinder
{
	internal static IConfigurationBuilder AddWindowsConfiguration(this IConfigurationBuilder builder)
	{
		builder.Add(new WindowsConfigurationSource());
		return builder;
	}

	private class WindowsConfigurationSource : IConfigurationSource
	{
		public IConfigurationProvider Build(IConfigurationBuilder builder)
		{
			var settings = ApplicationData.Current.RoamingSettings;
			
			return new WindowsConfigurationProvider(settings);
		}
	}

	internal class WindowsConfigurationProvider(ApplicationDataContainer settings) : ConfigurationProvider
	{
		public override void Load()
		{
			EnumerateContainer(string.Empty, settings);
		}

		private void EnumerateContainer(string path, ApplicationDataContainer container)
		{
			foreach (var (key, value) in container.Values)
			{
				var settingsKey = $"{path}:{key}";
				Data[settingsKey] = value?.ToString();
			}

			foreach (var childContainer in container.Containers)
			{
				EnumerateContainer($"{path}:{container.Name}", childContainer.Value);
			}
		}
	}
}
#endif
