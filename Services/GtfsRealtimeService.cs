using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using ProtoBuf;
using TransitRealtime;
using OpenDOSM.Malaysia.Models;

namespace OpenDOSM.Malaysia.Services;

    public class GtfsRealtimeService
    {

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private Timer? _pollingTimer;
    private string _currentEndpoint = "";

    public event Action? DataRefreshed;

    internal GtfsRealtimeService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _cache = new MemoryCache(new MemoryCacheOptions());
    }

    public List<StationModel> AllStations { get; private set; } = new();
    public List<VehiclePositionModel> LiveVehicles { get; private set; } = new();
    private List<VehiclePositionModel> _allActiveVehicles = new();
    public string SelectedRouteId { get; private set; } = "";

    public void StartPolling(string endpoint)
    {
        _currentEndpoint = endpoint;
        StopPolling();
        
        // Initial fetch
        _ = FetchGtfsRealtimeAsync();
        
        // Poll every 1 minute
        _pollingTimer = new Timer(async _ => await FetchGtfsRealtimeAsync(), null, 60000, 60000);
    }

    public void StopPolling()
    {
        if (_pollingTimer != null)
        {
            _pollingTimer.Dispose();
            _pollingTimer = null;
        }
    }

    public void SetSelectedRoute(string routeId)
    {
        SelectedRouteId = routeId;
        FilterVehicles();
    }

    private void FilterVehicles()
    {
        LiveVehicles.Clear();
        foreach (var v in _allActiveVehicles)
        {
            if (string.Equals(v.RouteId, SelectedRouteId, StringComparison.OrdinalIgnoreCase))
            {
                LiveVehicles.Add(v);
            }
        }
        DataRefreshed?.Invoke();
    }

    private async Task FetchGtfsRealtimeAsync()
    {
        try
        {
            string url;
            if (_currentEndpoint == "ktmb") url = "https://api.data.gov.my/gtfs-realtime/vehicle-position/ktmb";
            else if (_currentEndpoint.StartsWith("mybas")) url = $"https://api.data.gov.my/gtfs-realtime/vehicle-position/mybas?category={_currentEndpoint}";
            else url = $"https://api.data.gov.my/gtfs-realtime/vehicle-position/prasarana?category={_currentEndpoint}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            var feed = Serializer.Deserialize<FeedMessage>(stream);

            _allActiveVehicles.Clear();
            foreach (var entity in feed.Entities)
            {
                if (entity.Vehicle != null)
                {
                    var v = new VehiclePositionModel
                    {
                        VehicleId = entity.Vehicle.Vehicle?.Id ?? "Unknown",
                        RouteId = entity.Vehicle.Trip?.RouteId ?? "",
                        TripId = entity.Vehicle.Trip?.TripId ?? "",
                        Speed = Math.Round((entity.Vehicle.Position?.Speed ?? 0) * 3.6, 1),
                        Timestamp = DateTimeOffset.FromUnixTimeSeconds((long)entity.Vehicle.Timestamp).ToLocalTime().ToString("HH:mm:ss"),
                        CurrentStatus = entity.Vehicle.CurrentStatus.ToString(),
                    };

                    // Try to map StopSequence to StationIndex
                    // Note: GTFS-R StopSequence is 1-indexed, our StationIndex is 0-indexed
                    if (entity.Vehicle.CurrentStopSequence > 0)
                    {
                        v.CurrentStationIndex = (int)entity.Vehicle.CurrentStopSequence - 1;
                    }

                    if (entity.Vehicle.Position != null)
                    {
                        v.UpdateCoordinates(entity.Vehicle.Position.Latitude, entity.Vehicle.Position.Longitude);
                    }

                    _allActiveVehicles.Add(v);
                }
            }
            
            FilterVehicles();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching GTFS-R: {ex.Message}");
        }
    }

    public List<VehiclePositionModel> GetCachedVehicles(string endpoint, string query = "")
    {
        if (_cache.TryGetValue($"vehicles_{endpoint}", out List<VehiclePositionModel>? vehicles) && vehicles != null)
        {
            if (string.IsNullOrWhiteSpace(query))
                return vehicles;

            return vehicles.Where(v => 
                (v.RouteId?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (v.VehicleId?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (v.CurrentStatus?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
            ).ToList();
        }
        return new List<VehiclePositionModel>();
    }
}

