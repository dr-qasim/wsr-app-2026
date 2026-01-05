using System.Windows;
using System.Windows.Controls;
using VendingService.WPF.ViewModels;

namespace VendingService.WPF.Views;

public partial class ReportsView : UserControl
{
    private bool _isLoadedOnce;

    public ReportsView()
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

        if (DataContext is ReportsViewModel vm)
        {
            vm.LoadCommand.Execute(null);
        }
    }
}
