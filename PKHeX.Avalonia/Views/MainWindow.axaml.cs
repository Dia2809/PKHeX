using Avalonia.Controls;
using Avalonia.Input;
using PKHeX.Avalonia.ViewModels;

namespace PKHeX.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        var vm = new MainWindowViewModel();
        DataContext = vm;
        InitializeComponent();
        vm.TopLevel = TopLevel.GetTopLevel(this);
    }

    // ── Box grid keyboard navigation ─────────────────────────────────────────
    // WrapPanel doesn't implement INavigableContainer, so Avalonia's ListBox
    // can't move focus spatially on its own. We compute the target index here.

    private void BoxList_KeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not ListBox lb) return;
        if (DataContext is not MainWindowViewModel vm) return;

        int count = vm.CurrentBoxSlots.Count;
        if (count == 0) return;

        // Determine number of columns from box size (Gen 1-2 = 20 slots / 5 cols;
        // all later games = 30 slots / 6 cols; default to 6 for anything else).
        int cols = count switch { 20 => 5, 30 => 6, _ => 6 };

        int cur = lb.SelectedIndex < 0 ? 0 : lb.SelectedIndex;
        int next = e.Key switch
        {
            Key.Left  => (cur - 1 + count) % count,
            Key.Right => (cur + 1) % count,
            Key.Up    => (cur - cols + count) % count,
            Key.Down  => (cur + cols) % count,
            _ => -1,
        };

        if (next < 0) return;

        lb.SelectedIndex = next;
        lb.ScrollIntoView(lb.SelectedItem!);
        e.Handled = true;
    }

    // When the box ListBox gains focus with nothing selected, auto-select
    // the first occupied slot so the user can immediately navigate with D-pad.
    private void BoxList_GotFocus(object? sender, GotFocusEventArgs e)
    {
        if (sender is not ListBox lb) return;
        if (lb.SelectedIndex >= 0) return;
        if (DataContext is not MainWindowViewModel vm) return;
        if (vm.CurrentBoxSlots.Count > 0)
            lb.SelectedIndex = 0;
    }

    // ── Party list keyboard navigation ───────────────────────────────────────
    // The default VirtualizingStackPanel handles Up/Down, but L/R are dead.
    // Map them to Up/Down so a D-pad works regardless of orientation.

    private void PartyList_KeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not ListBox lb) return;
        if (DataContext is not MainWindowViewModel vm) return;

        int count = vm.PartySlots.Count;
        if (count == 0) return;

        int cur = lb.SelectedIndex < 0 ? 0 : lb.SelectedIndex;
        int next = e.Key switch
        {
            Key.Left  or Key.Up    => (cur - 1 + count) % count,
            Key.Right or Key.Down  => (cur + 1) % count,
            _ => -1,
        };

        if (next < 0) return;

        lb.SelectedIndex = next;
        e.Handled = true;
    }

    private void PartyList_GotFocus(object? sender, GotFocusEventArgs e)
    {
        if (sender is not ListBox lb) return;
        if (lb.SelectedIndex >= 0) return;
        if (DataContext is not MainWindowViewModel vm) return;
        if (vm.PartySlots.Count > 0)
            lb.SelectedIndex = 0;
    }
}
