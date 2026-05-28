using System.Net.Http;
using OpenDOSM.Malaysia.Services;

namespace OpenDOSM.Malaysia
{
    /// <summary>
    /// Main client for accessing Data.gov.my and OpenDOSM APIs.
    /// </summary>
    public class OpenDosmClient : IDisposable
    {
        /// <summary>
        /// Singleton instance of the client.
        /// </summary>
        public static OpenDosmClient Instance { get; } = new OpenDosmClient();

        /// <summary>
        /// Access static data endpoints (Catalogue, Static GTFS).
        /// </summary>
        public StaticApi Static { get; }

        /// <summary>
        /// Access realtime data endpoints (Weather, Realtime GTFS).
        /// </summary>
        public RealTimeApi RealTime { get; }

        private OpenDosmClient()
        {
            var httpClient = new HttpClient();
            Static = new StaticApi(httpClient);
            RealTime = new RealTimeApi(httpClient);
        }

        /// <summary>
        /// Initializes optional global configurations like API keys.
        /// </summary>
        /// <param name="googleMapsApiKey">Your Google Maps API key for Geocoding.</param>
        public void Initialize(string googleMapsApiKey)
        {
            GeocodingService.Instance.Initialize(googleMapsApiKey);
        }

        /// <summary>
        /// Disposes all nested services holding active resources or timers.
        /// </summary>
        public void Dispose()
        {
            RealTime?.Gtfs?.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    public class StaticApi
    {
        public GtfsStaticService Gtfs { get; }
        public IDataCatalogueService Catalogue { get; }

        internal StaticApi(HttpClient httpClient)
        {
            Gtfs = new GtfsStaticService(httpClient);
            Catalogue = new DataCatalogueService(httpClient);
        }
    }

    public class RealTimeApi
    {
        public GtfsRealtimeService Gtfs { get; }
        public IWeatherService Weather { get; }

        internal RealTimeApi(HttpClient httpClient)
        {
            Gtfs = new GtfsRealtimeService(httpClient);
            Weather = new WeatherService(httpClient);
        }
    }
}
