using System.Net.Http;
using OpenDOSM.Malaysia.Services;

namespace OpenDOSM.Malaysia
{
    public class OpenDosmClient
    {
        public static OpenDosmClient Instance { get; } = new OpenDosmClient();

        public StaticApi Static { get; }
        public RealTimeApi RealTime { get; }

        private OpenDosmClient()
        {
            var httpClient = new HttpClient();
            Static = new StaticApi(httpClient);
            RealTime = new RealTimeApi(httpClient);
        }

        public void Initialize(string googleMapsApiKey)
        {
            GeocodingService.Instance.Initialize(googleMapsApiKey);
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
