using System.Windows;
using System.Windows.Controls;
using VendingService.WPF.ViewModels;

namespace VendingService.WPF.Views;

public partial class AdditionalView : UserControl
{
    private bool _isLoadedOnce;

    public AdditionalView()
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

        if (DataContext is AdditionalViewModel vm)
        {
            vm.LoadCommand.Execute(null);
        }
    }
}
