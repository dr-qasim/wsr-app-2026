using System.Windows;
using System.Windows.Controls;
using VendingService.WPF.ViewModels;

namespace VendingService.WPF.Views;

public partial class MonitorView : UserControl
{
    public MonitorView()
    {
        InitializeComponent();
    }

    private void UserControl_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MonitorViewModel vm)
        {
            vm.Start();
        }
    }

    private void UserControl_OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MonitorViewModel vm)
        {
            vm.Stop();
        }
    }

    private void DataGrid_OnLoadingRow(object sender, DataGridRowEventArgs e)
    {
        e.Row.Header = e.Row.GetIndex() + 1;
    }
}
