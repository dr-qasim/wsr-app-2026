using System.Windows;
using System.Windows.Controls;
using VendingService.WPF.ViewModels;

namespace VendingService.WPF.Views;

public partial class InventoryView : UserControl
{
    private bool _isLoadedOnce;

    public InventoryView()
    {
        InitializeComponent();
    }

    private void UserControl_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_isLoadedOnce)
        {
            return;
        }

        _isLoadedOnce = true;

        if (DataContext is InventoryViewModel vm)
        {
            vm.LoadCommand.Execute(null);
        }
    }
}
