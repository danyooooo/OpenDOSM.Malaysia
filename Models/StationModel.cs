namespace OpenDOSM.Malaysia.Models;

public partial class StationModel : System.ComponentModel.INotifyPropertyChanged
{
    private bool _hasBus;
    private string _directionArrow = "";

    public string Name { get; set; } = "";
    public int Index { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public bool HasBus 
    { 
        get => _hasBus; 
        set 
        { 
            _hasBus = value; 
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(HasBus))); 
        } 
    }

    public string DirectionArrow 
    { 
        get => _directionArrow; 
        set 
        { 
            _directionArrow = value; 
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(DirectionArrow))); 
        } 
    }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
}

