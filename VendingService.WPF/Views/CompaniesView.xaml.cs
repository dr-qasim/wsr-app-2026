using System.Windows;
using System.Windows.Controls;
using VendingService.WPF.Contracts.Companies;
using VendingService.WPF.ViewModels;

namespace VendingService.WPF.Views;

public partial class CompaniesView : UserControl
{
    private bool _isLoadedOnce;

    public CompaniesView()
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

        if (DataContext is CompaniesViewModel vm)
        {
            vm.LoadCommand.Execute(null);
        }
    }

    private void AddButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not CompaniesViewModel vm)
        {
            return;
        }

        var window = new CompanyEditWindow
        {
            Owner = Window.GetWindow(this)
        };

        if (window.ShowDialog() == true)
        {
            vm.LoadCommand.Execute(null);
        }
    }

    private void EditButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not CompanyListItem item)
        {
            return;
        }

        if (DataContext is not CompaniesViewModel vm)
        {
            return;
        }

        var window = new CompanyEditWindow(item.CompanyId)
        {
            Owner = Window.GetWindow(this)
        };

        if (window.ShowDialog() == true)
        {
            vm.LoadCommand.Execute(null);
        }
    }

    private void DeleteButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not CompaniesViewModel vm)
        {
            return;
        }

        if (((FrameworkElement)sender).DataContext is not CompanyListItem item)
        {
            return;
        }

        vm.DeleteCommand.Execute(item.CompanyId);
    }
}

