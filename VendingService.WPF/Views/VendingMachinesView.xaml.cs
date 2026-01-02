using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using VendingService.WPF.Contracts.VendingMachines;
using VendingService.WPF.ViewModels;

namespace VendingService.WPF.Views;

public partial class VendingMachinesView : UserControl
{
    private bool _isLoadedOnce;

    public VendingMachinesView()
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

        if (DataContext is VendingMachinesViewModel vm)
        {
            vm.LoadCommand.Execute(null);
        }
    }

    private void AddButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not VendingMachinesViewModel vm)
        {
            return;
        }

        var window = new VendingMachineEditWindow
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
        if (((FrameworkElement)sender).DataContext is not VendingMachineListItem item)
        {
            return;
        }

        if (DataContext is not VendingMachinesViewModel vm)
        {
            return;
        }

        var window = new VendingMachineEditWindow(item.VendingMachineId)
        {
            Owner = Window.GetWindow(this)
        };

        if (window.ShowDialog() == true)
        {
            vm.LoadCommand.Execute(null);
        }
    }

    private void DetachButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not VendingMachinesViewModel vm)
        {
            return;
        }

        if (((FrameworkElement)sender).DataContext is not VendingMachineListItem item)
        {
            return;
        }

        vm.DetachModemCommand.Execute(item.VendingMachineId);
    }

    private void DeleteButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not VendingMachinesViewModel vm)
        {
            return;
        }

        if (((FrameworkElement)sender).DataContext is not VendingMachineListItem item)
        {
            return;
        }

        vm.DeleteCommand.Execute(item.VendingMachineId);
    }

    private void ExportButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        button.ContextMenu?.IsOpen = true;
    }

    private void ExportCsv_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not VendingMachinesViewModel vm)
        {
            return;
        }

        var dlg = new SaveFileDialog
        {
            Filter = "CSV (*.csv)|*.csv",
            FileName = "VendingMachines.csv"
        };

        if (dlg.ShowDialog() != true)
        {
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("Id;Name;Manufacturer;Model;Company;Modem;Address;Place;CommissioningDate");

        foreach (var x in vm.Items)
        {
            var modem = x.ModemId == -1 ? "-1" : (x.ModemNumber ?? "");
            sb.AppendLine(string.Join(';', new[]
            {
                x.VendingMachineId.ToString(),
                EscapeCsv(x.Name),
                EscapeCsv(x.ManufacturerName),
                EscapeCsv(x.ModelName),
                EscapeCsv(x.CompanyName ?? ""),
                EscapeCsv(modem),
                EscapeCsv(x.Address),
                EscapeCsv(x.Place),
                x.CommissioningDate.ToString("yyyy-MM-dd")
            }));
        }

        File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
        MessageBox.Show("Экспорт CSV выполнен.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ExportHtml_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not VendingMachinesViewModel vm)
        {
            return;
        }

        var dlg = new SaveFileDialog
        {
            Filter = "HTML (*.html)|*.html",
            FileName = "VendingMachines.html"
        };

        if (dlg.ShowDialog() != true)
        {
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html><html><head><meta charset=\"utf-8\"/>");
        sb.AppendLine("<style>table{border-collapse:collapse}td,th{border:1px solid #ccc;padding:6px 10px}</style>");
        sb.AppendLine("</head><body>");
        sb.AppendLine("<h2>Торговые автоматы</h2>");
        sb.AppendLine("<table><thead><tr>");
        sb.AppendLine("<th>ID</th><th>Название</th><th>Модель</th><th>Компания</th><th>Модем</th><th>Адрес</th><th>Место</th><th>В работе с</th>");
        sb.AppendLine("</tr></thead><tbody>");

        foreach (var x in vm.Items)
        {
            var modem = x.ModemId == -1 ? "-1" : (x.ModemNumber ?? "");
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{x.VendingMachineId}</td>");
            sb.AppendLine($"<td>{Html(x.Name)}</td>");
            sb.AppendLine($"<td>{Html($"{x.ManufacturerName} {x.ModelName}")}</td>");
            sb.AppendLine($"<td>{Html(x.CompanyName ?? "")}</td>");
            sb.AppendLine($"<td>{Html(modem)}</td>");
            sb.AppendLine($"<td>{Html(x.Address)}</td>");
            sb.AppendLine($"<td>{Html(x.Place)}</td>");
            sb.AppendLine($"<td>{x.CommissioningDate:yyyy-MM-dd}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody></table></body></html>");

        File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
        MessageBox.Show("Экспорт HTML выполнен.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ExportPdf_OnClick(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("PDF экспорт добавим на следующем шаге (можно через небольшую библиотеку для генерации PDF).", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static string EscapeCsv(string value)
    {
        if (!value.Contains(';') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private static string Html(string value)
        => System.Net.WebUtility.HtmlEncode(value);
}
