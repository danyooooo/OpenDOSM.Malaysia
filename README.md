# OpenDOSM.Malaysia

**OpenDOSM.Malaysia** is a robust, asynchronous C# .NET library that acts as a comprehensive wrapper for the [Data.gov.my](https://developer.data.gov.my/) and [OpenDOSM](https://open.dosm.gov.my/) APIs. It provides strongly-typed models, intelligent fallback mechanisms, and an easy-to-use Singleton client for querying Malaysia's national open data endpoints.

## Features

- **GTFS Realtime & Static**: Parse and query live public transportation telemetry (e.g., KTMB, Prasarana) alongside static route infrastructures.
- **Weather API Integration**: Fetch 7-day live weather forecasts and warnings (including earthquake and tsunami advisories) natively from the Malaysian Meteorological Department (MET) endpoints.
- **Data Catalogue Querying**: Fetch arbitrary datasets from OpenDOSM's massive static data catalogue (such as daily exchange rates).
- **Native Geocoding**: Built-in support for Google Geocoding to automatically resolve GTFS coordinate telemetry to readable street names, complete with mock-fallbacks for local development.

## Installation & Setup

1. Add the project reference to your C# application.
2. Ensure you are targeting **.NET 8.0** or above.
3. Initialize the library in your startup configuration (e.g., `App.xaml.cs` or `Program.cs`):

```csharp
// Optional: Pass your Google Maps API Key to enable real-world reverse geocoding.
// If left blank, the library will safely fallback to mock geocoded addresses.
OpenDosmClient.Instance.Initialize("YOUR_GOOGLE_MAPS_API_KEY");
```

## Quick Start

### 1. Fetching Weather Forecasts
Fetch 7-day forecasts seamlessly parsed into a list of natively typed objects:
```csharp
var forecasts = await OpenDosmClient.Instance.RealTime.Weather.GetForecastsAsync(limit: 7, locationContains: "Ds001@location__location_id");
foreach (var day in forecasts)
{
    Console.WriteLine($"{day.DayOfWeek}: {day.MorningForecast} (Max: {day.MaxTemp}°C)");
}
```

### 2. Fetching Realtime Transport Data
Stream live vehicle positions across Malaysia's transit system:
```csharp
var positions = await OpenDosmClient.Instance.RealTime.Gtfs.GetVehiclePositionsAsync("ktmb");
foreach (var pos in positions)
{
    Console.WriteLine($"Train ID: {pos.VehicleId} is at {pos.LocationName} moving at {pos.Speed}km/h");
}
```

### 3. Fetching Static Data (e.g. Exchange Rates)
```csharp
var rates = await OpenDosmClient.Instance.Static.Catalogue.GetDatasetAsync<ExchangeRateModel>("exchangerates_daily_0900", limit: 1);
var today = rates.FirstOrDefault();
Console.WriteLine($"1 USD = {today.Usd} MYR");
```

## Architecture

The `OpenDosmClient` splits endpoints logically into two sections:
- `OpenDosmClient.Instance.RealTime` (Weather, GTFS Live Data)
- `OpenDosmClient.Instance.Static` (GTFS Routes/Stops, DOSM Data Catalogue)

## Background Polling & Resource Management

For Realtime GTFS, the library supports automated background polling. You can fluently configure the polling rate (minimum of 30 seconds) and easily stop operations to release active memory/timers when shutting down components.

```csharp
// Configure polling interval to 45 seconds and start
OpenDosmClient.Instance.RealTime.Gtfs
    .WithPingLimitInSeconds(45)
    .StartPolling("ktmb");

// Stop polling and dispose all background timers manually
OpenDosmClient.Instance.Dispose();
```
