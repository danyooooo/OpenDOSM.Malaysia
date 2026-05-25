using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using OpenDOSM.Malaysia.Models;

namespace OpenDOSM.Malaysia.Services;

    public class GtfsStaticService
    {

    private readonly string _cacheDirectory;
    private readonly HttpClient _httpClient;

    internal GtfsStaticService(HttpClient httpClient)
    {
        _cacheDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TransportApp", "GtfsCache");
        if (!Directory.Exists(_cacheDirectory))
        {
            Directory.CreateDirectory(_cacheDirectory);
        }
        _httpClient = httpClient;
    }

    public async Task EnsureGtfsDataAsync(string category, bool forceRefresh = false)
    {
        string zipPath = Path.Combine(_cacheDirectory, $"{category}.zip");
        string extractPath = Path.Combine(_cacheDirectory, category);

        if (forceRefresh || !Directory.Exists(extractPath) || !File.Exists(Path.Combine(extractPath, "stops.txt")))
        {
            string url;
            if (category == "ktmb") url = "https://api.data.gov.my/gtfs-static/ktmb";
            else if (category.StartsWith("mybas")) url = $"https://api.data.gov.my/gtfs-static/mybas?category={category}";
            else url = $"https://api.data.gov.my/gtfs-static/prasarana?category={category}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            using (var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await response.Content.CopyToAsync(fs);
            }

            if (Directory.Exists(extractPath))
            {
                Directory.Delete(extractPath, true);
            }
            
            ZipFile.ExtractToDirectory(zipPath, extractPath);
            File.Delete(zipPath);
        }
    }

    private Dictionary<string, int> ParseHeader(string[] row)
    {
        var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for(int i = 0; i < row.Length; i++)
        {
            dict[row[i].Trim('\xFEFF', '\"', ' ')] = i; // strip BOM and quotes
        }
        return dict;
    }

    public async Task<List<string>> GetAllRoutesAsync(string category, bool forceRefresh = false)
    {
        await EnsureGtfsDataAsync(category, forceRefresh);
        string extractPath = Path.Combine(_cacheDirectory, category);
        
        var routes = new List<string>();
        using (var reader = new StreamReader(Path.Combine(extractPath, "routes.txt")))
        {
            var headerDict = ParseHeader(SplitCsvLine(reader.ReadLine() ?? ""));
            int shortNameIdx = headerDict.GetValueOrDefault("route_short_name", -1);
            int longNameIdx = headerDict.GetValueOrDefault("route_long_name", -1);

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var row = SplitCsvLine(line);
                string rShort = shortNameIdx >= 0 && shortNameIdx < row.Length ? row[shortNameIdx] : "";
                string rLong = longNameIdx >= 0 && longNameIdx < row.Length ? row[longNameIdx] : "";
                
                string combined = !string.IsNullOrEmpty(rShort) ? rShort : rLong;
                if (!string.IsNullOrEmpty(rShort) && !string.IsNullOrEmpty(rLong) && rShort != rLong) combined = $"{rShort} - {rLong}";
                
                if (!string.IsNullOrEmpty(combined)) routes.Add(combined);
            }
        }
        return routes.Distinct().OrderBy(r => r).ToList();
    }

    public async Task<(string RouteId, List<StationModel> Stations)> GetStationsForRouteAsync(string category, string routeName, bool forceRefresh = false)
    {
        await EnsureGtfsDataAsync(category, forceRefresh);
        string extractPath = Path.Combine(_cacheDirectory, category);

        // 1. Find Route ID
        string routeId = "";
        using (var reader = new StreamReader(Path.Combine(extractPath, "routes.txt")))
        {
            var headerDict = ParseHeader(SplitCsvLine(reader.ReadLine() ?? ""));
            int shortNameIdx = headerDict.GetValueOrDefault("route_short_name", -1);
            int longNameIdx = headerDict.GetValueOrDefault("route_long_name", -1);
            int routeIdIdx = headerDict.GetValueOrDefault("route_id", -1);

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var row = SplitCsvLine(line);
                string rShort = shortNameIdx >= 0 && shortNameIdx < row.Length ? row[shortNameIdx] : "";
                string rLong = longNameIdx >= 0 && longNameIdx < row.Length ? row[longNameIdx] : "";
                
                string combined = !string.IsNullOrEmpty(rShort) ? rShort : rLong;
                if (!string.IsNullOrEmpty(rShort) && !string.IsNullOrEmpty(rLong) && rShort != rLong) combined = $"{rShort} - {rLong}";

                if (combined.Equals(routeName, StringComparison.OrdinalIgnoreCase) || 
                    rShort.Equals(routeName, StringComparison.OrdinalIgnoreCase) || 
                    rLong.Equals(routeName, StringComparison.OrdinalIgnoreCase))
                {
                    if (routeIdIdx >= 0 && routeIdIdx < row.Length)
                    {
                        routeId = row[routeIdIdx];
                        break;
                    }
                }
            }
        }

        if (string.IsNullOrEmpty(routeId)) return ("", new List<StationModel>());

        // 2. Find ALL Trip IDs for this Route
        var tripIds = new HashSet<string>();
        using (var reader = new StreamReader(Path.Combine(extractPath, "trips.txt")))
        {
            var headerDict = ParseHeader(SplitCsvLine(reader.ReadLine() ?? ""));
            int routeIdIdx = headerDict.GetValueOrDefault("route_id", -1);
            int tripIdIdx = headerDict.GetValueOrDefault("trip_id", -1);

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var row = SplitCsvLine(line);
                if (routeIdIdx >= 0 && row.Length > routeIdIdx && row[routeIdIdx] == routeId)
                {
                    if (tripIdIdx >= 0 && row.Length > tripIdIdx)
                    {
                        tripIds.Add(row[tripIdIdx]);
                    }
                }
            }
        }

        if (tripIds.Count == 0) return ("", new List<StationModel>());

        // 3. Find the Trip with the MOST stops
        var tripStopsDict = new Dictionary<string, List<(int Sequence, string StopId)>>();
        using (var reader = new StreamReader(Path.Combine(extractPath, "stop_times.txt")))
        {
            var headerDict = ParseHeader(SplitCsvLine(reader.ReadLine() ?? ""));
            int tripIdIdx = headerDict.GetValueOrDefault("trip_id", -1);
            int stopIdIdx = headerDict.GetValueOrDefault("stop_id", -1);
            int seqIdx = headerDict.GetValueOrDefault("stop_sequence", -1);

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var row = SplitCsvLine(line);
                if (tripIdIdx >= 0 && row.Length > tripIdIdx)
                {
                    string tId = row[tripIdIdx];
                    if (tripIds.Contains(tId))
                    {
                        if (seqIdx >= 0 && row.Length > seqIdx && stopIdIdx >= 0 && row.Length > stopIdIdx)
                        {
                            if (int.TryParse(row[seqIdx], out int seq))
                            {
                                if (!tripStopsDict.ContainsKey(tId)) tripStopsDict[tId] = new List<(int, string)>();
                                tripStopsDict[tId].Add((seq, row[stopIdIdx]));
                            }
                        }
                    }
                }
            }
        }

        string bestTripId = "";
        int maxStops = -1;
        foreach (var kvp in tripStopsDict)
        {
            if (kvp.Value.Count > maxStops)
            {
                maxStops = kvp.Value.Count;
                bestTripId = kvp.Key;
            }
        }

        if (string.IsNullOrEmpty(bestTripId)) return ("", new List<StationModel>());
        var tripStops = tripStopsDict[bestTripId].OrderBy(x => x.Sequence).ToList();

        // 4. Map Stop IDs to Station details
        var stopsDict = new Dictionary<string, (string Name, double Lat, double Lon)>();
        using (var reader = new StreamReader(Path.Combine(extractPath, "stops.txt")))
        {
            var headerDict = ParseHeader(SplitCsvLine(reader.ReadLine() ?? ""));
            int stopIdIdx = headerDict.GetValueOrDefault("stop_id", -1);
            int stopNameIdx = headerDict.GetValueOrDefault("stop_name", -1);
            int latIdx = headerDict.GetValueOrDefault("stop_lat", -1);
            int lonIdx = headerDict.GetValueOrDefault("stop_lon", -1);

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var row = SplitCsvLine(line);
                if (stopIdIdx >= 0 && row.Length > stopIdIdx && latIdx >= 0 && row.Length > latIdx && lonIdx >= 0 && row.Length > lonIdx)
                {
                    if (double.TryParse(row[latIdx], out double lat) && double.TryParse(row[lonIdx], out double lon))
                    {
                        stopsDict[row[stopIdIdx]] = (row[stopNameIdx], lat, lon);
                    }
                }
            }
        }

        var stations = new List<StationModel>();
        for (int i = 0; i < tripStops.Count; i++)
        {
            var stopId = tripStops[i].StopId;
            if (stopsDict.TryGetValue(stopId, out var stopData))
            {
                stations.Add(new StationModel 
                { 
                    Index = i, 
                    Name = stopData.Name, 
                    Latitude = stopData.Lat, 
                    Longitude = stopData.Lon 
                });
            }
        }

        return (routeId, stations);
    }

    private string[] SplitCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        int start = 0;
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == '\"')
            {
                inQuotes = !inQuotes;
            }
            else if (line[i] == ',' && !inQuotes)
            {
                result.Add(line.Substring(start, i - start).Trim('\"'));
                start = i + 1;
            }
        }
        result.Add(line.Substring(start).Trim('\"'));
        return result.ToArray();
    }
}

