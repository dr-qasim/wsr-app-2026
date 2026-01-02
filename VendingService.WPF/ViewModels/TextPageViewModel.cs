namespace VendingService.WPF.ViewModels;

public sealed class TextPageViewModel(string title, string message) : ObservableObject
{
    public string Title { get; } = title;
    public string Message { get; } = message;
}

