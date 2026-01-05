using System.Windows;
using System.Windows.Controls;
using VendingService.WPF.ViewModels;

namespace VendingService.WPF.Views;

public partial class ModemsView : UserControl
{
    private bool _isLoadedOnce;

    public ModemsView()
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

        if (DataContext is ModemsViewModel vm)
        {
            vm.LoadCommand.Execute(null);
        }
    }
}
