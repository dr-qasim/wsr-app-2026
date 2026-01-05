using System.Windows;
using System.Windows.Controls;
using VendingService.WPF.ViewModels;

namespace VendingService.WPF.Views;

public partial class UsersView : UserControl
{
    private bool _isLoadedOnce;

    public UsersView()
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

        if (DataContext is UsersViewModel vm)
        {
            vm.LoadCommand.Execute(null);
        }
    }
}
