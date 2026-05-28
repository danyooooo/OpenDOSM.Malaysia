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

/// <summary>
/// Service for fetching and managing GTFS Realtime vehicle telemetry data.
/// </summary>
public class GtfsRealtimeService : IDisposable
{

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private Timer? _pollingTimer;
    private string _currentEndpoint = "";
    private int _pingLimitMs = 60000;

    /// <summary>
    /// Event triggered when vehicle data has been refreshed.
    /// </summary>
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

    /// <summary>
    /// Configures the polling interval for the realtime data stream.
    /// </summary>
    /// <param name="seconds">The interval in seconds.</param>
    /// <returns>The current <see cref="GtfsRealtimeService"/> instance for fluent chaining.</returns>
    public GtfsRealtimeService WithPingLimitInSeconds(int seconds)
    {
        // Enforce a minimum of 30 seconds to prevent API abuse
        int safeSeconds = Math.Max(30, seconds);
        
        _pingLimitMs = safeSeconds * 1000;
        if (_pollingTimer != null)
        {
            _pollingTimer.Change(_pingLimitMs, _pingLimitMs);
        }
        return this;
    }

    /// <summary>
    /// Starts polling the GTFS Realtime endpoint at the configured interval.
    /// </summary>
    /// <param name="endpoint">The category endpoint (e.g., 'ktmb', 'prasarana').</param>
    public void StartPolling(string endpoint)
    {
        _currentEndpoint = endpoint;
        StopPolling();
        
        // Initial fetch
        _ = FetchGtfsRealtimeAsync();
        
        // Poll at the configured interval
        _pollingTimer = new Timer(async _ => await FetchGtfsRealtimeAsync(), null, _pingLimitMs, _pingLimitMs);
    }

    /// <summary>
    /// Stops the active polling timer.
    /// </summary>
    public void StopPolling()
    {
        if (_pollingTimer != null)
        {
            _pollingTimer.Dispose();
            _pollingTimer = null;
        }
    }

    /// <summary>
    /// Disposes the polling timer and releases resources.
    /// </summary>
    public void Dispose()
    {
        StopPolling();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Sets the currently selected route ID and filters the active vehicles to only show vehicles on this route.
    /// </summary>
    /// <param name="routeId">The GTFS route ID to filter by.</param>
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

    /// <summary>
    /// Gets cached vehicle positions optionally filtered by a search query.
    /// </summary>
    /// <param name="endpoint">The category endpoint.</param>
    /// <param name="query">An optional search term to filter by route ID, vehicle ID, or status.</param>
    /// <returns>A list of cached vehicle positions.</returns>
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

