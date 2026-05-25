using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using OpenDOSM.Malaysia.Models;

namespace OpenDOSM.Malaysia.Services
{
    public class WeatherService : IWeatherService
    {
        private readonly HttpClient _httpClient;

        public WeatherService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<WeatherForecast>> GetForecastsAsync(int limit = -1, string? locationContains = null, DateTime? dateStart = null, DateTime? dateEnd = null)
        {
            var url = "https://api.data.gov.my/weather/forecast?";
            var query = new List<string>();

            if (limit > 0)
                query.Add($"limit={limit}");
            if (!string.IsNullOrEmpty(locationContains))
                query.Add($"contains={Uri.EscapeDataString(locationContains)}");
            if (dateStart.HasValue)
                query.Add($"date_start={dateStart.Value:yyyy-MM-dd}@date");
            if (dateEnd.HasValue)
                query.Add($"date_end={dateEnd.Value:yyyy-MM-dd}@date");

            if (query.Count > 0)
                url += string.Join("&", query);

            var response = await _httpClient.GetFromJsonAsync<List<WeatherForecast>>(url);
            return response ?? new List<WeatherForecast>();
        }

        public async Task<List<WeatherWarning>> GetWarningsAsync()
        {
            var url = "https://api.data.gov.my/weather/warning";
            var response = await _httpClient.GetFromJsonAsync<List<WeatherWarning>>(url);
            return response ?? new List<WeatherWarning>();
        }

        public async Task<List<WeatherWarning>> GetEarthquakeWarningsAsync()
        {
            var url = "https://api.data.gov.my/weather/warning/earthquake";
            var response = await _httpClient.GetFromJsonAsync<List<WeatherWarning>>(url);
            return response ?? new List<WeatherWarning>();
        }
    }
}
