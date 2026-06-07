using Avalonia.Controls;
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
}
