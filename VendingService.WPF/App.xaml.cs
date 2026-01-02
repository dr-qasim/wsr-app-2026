using System.Windows;
using VendingService.WPF.Services;

namespace VendingService.WPF;

public partial class App : Application
{
    public AppServices Services { get; } = new();
}

