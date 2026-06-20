using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AssetStudio.Maui;

internal sealed class AssetListColumnWidths : INotifyPropertyChanged
{
    public static readonly AssetListColumnWidths Instance = new();

    private double container = 100;
    private double type = 90;
    private double pathId = 70;
    private double size = 60;

    public double Container
    {
        get => container;
        set { container = value; OnPropertyChanged(); }
    }

    public double Type
    {
        get => type;
        set { type = value; OnPropertyChanged(); }
    }

    public double PathID
    {
        get => pathId;
        set { pathId = value; OnPropertyChanged(); }
    }

    public double Size
    {
        get => size;
        set { size = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
