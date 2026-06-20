using System.ComponentModel;
using System.Runtime.CompilerServices;
using AssetStudioCore;

namespace AssetStudio.Maui;

internal sealed class SceneTreeRow : INotifyPropertyChanged
{
    public AssetNode Node { get; }
    public int Depth { get; }
    public bool HasChildren => Node.Nodes.Count > 0;
    public double Indent => Depth * 16;
    public string ExpanderGlyph => !HasChildren ? "" : (IsExpanded ? "▼" : "▶");

    private bool isExpanded;
    public bool IsExpanded
    {
        get => isExpanded;
        set
        {
            isExpanded = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ExpanderGlyph));
        }
    }

    public SceneTreeRow(AssetNode node, int depth)
    {
        Node = node;
        Depth = depth;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
