using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
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
        if (DataContext is not VendingMachinesViewModel vm)
        {
            return;
        }

        var dlg = new SaveFileDialog
        {
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = "VendingMachines.pdf"
        };

        if (dlg.ShowDialog() != true)
        {
            return;
        }

        QuestPDF.Settings.License = LicenseType.Community;

        var data = vm.Items.ToList();

        Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header()
                        .Text("Торговые автоматы")
                        .FontSize(16)
                        .SemiBold();

                    page.Content().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(40);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("ID");
                            header.Cell().Element(HeaderCell).Text("Название");
                            header.Cell().Element(HeaderCell).Text("Модель");
                            header.Cell().Element(HeaderCell).Text("Компания");
                            header.Cell().Element(HeaderCell).Text("Модем");
                            header.Cell().Element(HeaderCell).Text("Адрес");
                            header.Cell().Element(HeaderCell).Text("Место");
                            header.Cell().Element(HeaderCell).Text("В работе с");
                        });

                        foreach (var x in data)
                        {
                            var modem = x.ModemId == -1 ? "-1" : (x.ModemNumber ?? "");
                            table.Cell().Element(BodyCell).Text(x.VendingMachineId.ToString());
                            table.Cell().Element(BodyCell).Text(x.Name);
                            table.Cell().Element(BodyCell).Text($"{x.ManufacturerName} {x.ModelName}");
                            table.Cell().Element(BodyCell).Text(x.CompanyName ?? "");
                            table.Cell().Element(BodyCell).Text(modem);
                            table.Cell().Element(BodyCell).Text(x.Address);
                            table.Cell().Element(BodyCell).Text(x.Place);
                            table.Cell().Element(BodyCell).Text(x.CommissioningDate.ToString("yyyy-MM-dd"));
                        }
                    });
                });
            })
            .GeneratePdf(dlg.FileName);

        MessageBox.Show("Экспорт PDF выполнен.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);

        static IContainer HeaderCell(IContainer container)
            => container.Background(Colors.Grey.Lighten3).Padding(4).Border(1).BorderColor(Colors.Grey.Lighten1);

        static IContainer BodyCell(IContainer container)
            => container.Padding(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten3);
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
