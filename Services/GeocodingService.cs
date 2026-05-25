using System;
using System.Linq;
using System.Threading.Tasks;
using Geocoding.Google;

namespace OpenDOSM.Malaysia.Services;

public class GeocodingService
{
    private static readonly GeocodingService _instance = new();
    public static GeocodingService Instance => _instance;

    private GoogleGeocoder _geocoder;

    private GeocodingService()
    {
        _geocoder = new GoogleGeocoder() { ApiKey = "YOUR_GOOGLE_MAPS_API_KEY" };
    }

    internal void Initialize(string apiKey)
    {
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            _geocoder = new GoogleGeocoder() { ApiKey = apiKey };
        }
    }

    public async Task<string> ReverseGeocodeAsync(double latitude, double longitude)
    {
        try
        {
            if (_geocoder.ApiKey == "YOUR_GOOGLE_MAPS_API_KEY")
            {
                // Fallback for mock if user hasn't provided an API key
                await Task.Delay(500); // Simulate network latency
                return $"Mock Street (Lat: {Math.Round(latitude, 4)}, Lng: {Math.Round(longitude, 4)})";
            }

            var addresses = await _geocoder.ReverseGeocodeAsync(latitude, longitude);
            var address = addresses.FirstOrDefault();
            
            if (address != null)
            {
                // Format the address nicely
                return !string.IsNullOrEmpty(address.FormattedAddress) ? address.FormattedAddress : "-";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Geocoding error: {ex.Message}");
        }

        return "-";
    }
}

