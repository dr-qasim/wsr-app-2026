using System.Windows;
using VendingService.WPF.ViewModels;

namespace VendingService.WPF.Views;

public partial class CompanyEditWindow : Window
{
    public CompanyEditWindow(int? companyId = null)
    {
        InitializeComponent();

        var vm = new CompanyEditViewModel(companyId);
        vm.Saved += (_, _) =>
        {
            DialogResult = true;
            Close();
        };

        DataContext = vm;
    }

    private void Window_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is CompanyEditViewModel vm)
        {
            vm.InitializeCommand.Execute(null);
        }
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

