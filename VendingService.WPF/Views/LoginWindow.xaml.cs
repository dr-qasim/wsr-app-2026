using System.Windows;
using System.Windows.Controls;
using VendingService.WPF.ViewModels;

namespace VendingService.WPF.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();

        if (DataContext is LoginViewModel vm)
        {
            vm.LoggedIn += (_, _) =>
            {
                var mainWindow = new MainWindow();
                mainWindow.Show();
                Close();
            };
        }
    }

    private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is not LoginViewModel vm)
        {
            return;
        }

        vm.Password = ((PasswordBox)sender).Password;
    }
}

