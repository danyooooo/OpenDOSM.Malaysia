namespace OpenDOSM.Malaysia.Models;

public class VehiclePositionModel : System.ComponentModel.INotifyPropertyChanged
{
    private int _currentStationIndex;
    private double _latitude;
    private double _longitude;
    private string _locationName = "Locating...";

    public string VehicleId { get; set; } = "";
    public string RouteId { get; set; } = "";
    public string TripId { get; set; } = "";
    public double Speed { get; set; }
    public string Timestamp { get; set; } = "";
    public string CurrentStatus { get; set; } = "";

    public int CurrentStationIndex
    {
        get => _currentStationIndex;
        set
        {
            _currentStationIndex = value;
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(CurrentStationIndex)));
        }
    }

    public double Latitude => _latitude;
    public double Longitude => _longitude;

    public string LocationName
    {
        get => _locationName;
        private set 
        { 
            _locationName = value; 
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(LocationName)));
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(FormattedLocation)));
        }
    }

    public void UpdateCoordinates(double lat, double lon)
    {
        _latitude = lat;
        _longitude = lon;
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Latitude)));
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Longitude)));
        _ = UpdateLocationAsync();
    }

    private async System.Threading.Tasks.Task UpdateLocationAsync()
    {
        if (_latitude != 0 && _longitude != 0)
        {
            LocationName = await OpenDOSM.Malaysia.Services.GeocodingService.Instance.ReverseGeocodeAsync(_latitude, _longitude);
        }
    }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    
    public string FormattedLocation => LocationName;
    public string FormattedSpeed => Speed != 0 ? $"{Speed} km/h" : "Idle";
}

