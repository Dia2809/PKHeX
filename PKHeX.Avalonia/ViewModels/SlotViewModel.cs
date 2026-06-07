using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using PKHeX.Core;

namespace PKHeX.Avalonia.ViewModels;

public partial class SlotViewModel : ViewModelBase
{
    public int Box { get; }
    public int Slot { get; }
    public PKM Entity { get; private set; }

    [ObservableProperty] private string _displayText = "";
    [ObservableProperty] private string _toolTipText = "";
    [ObservableProperty] private IBrush _background = Brushes.Transparent;
    [ObservableProperty] private bool _isEmpty = true;

    private static readonly IBrush ShinyBrush = new SolidColorBrush(Color.Parse("#FFD700"));
    private static readonly IBrush OccupiedBrush = new SolidColorBrush(Color.Parse("#D0E8FF"));

    public SlotViewModel(int box, int slot, PKM entity, string[] speciesNames)
    {
        Box = box;
        Slot = slot;
        Entity = entity;
        Refresh(speciesNames);
    }

    public void Update(PKM entity, string[] speciesNames)
    {
        Entity = entity;
        Refresh(speciesNames);
    }

    private void Refresh(string[] speciesNames)
    {
        if (Entity.Species == 0)
        {
            IsEmpty = true;
            DisplayText = "";
            ToolTipText = "Empty";
            Background = Brushes.Transparent;
            return;
        }

        IsEmpty = false;
        string name = Entity.Species < speciesNames.Length ? speciesNames[Entity.Species] : $"#{Entity.Species}";
        DisplayText = name;
        ToolTipText = $"Lv.{Entity.CurrentLevel} {name}{(Entity.IsShiny ? " ★" : "")}";
        Background = Entity.IsShiny ? ShinyBrush : OccupiedBrush;
    }
}
