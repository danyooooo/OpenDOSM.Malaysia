using System.Collections.Generic;
using System.Threading.Tasks;
using OpenDOSM.Malaysia.Models;

namespace OpenDOSM.Malaysia.Services
{
    public interface IWeatherService
    {
        Task<List<WeatherForecast>> GetForecastsAsync(int limit = -1, string? locationContains = null, DateTime? dateStart = null, DateTime? dateEnd = null);
        Task<List<WeatherWarning>> GetWarningsAsync();
        Task<List<WeatherWarning>> GetEarthquakeWarningsAsync();
    }
}
