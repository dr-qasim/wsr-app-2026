namespace VendingService.WPF.ViewModels;

public sealed class ToastViewModel : ObservableObject
{
    public ToastLevel Level { get; }
    public string Title { get; }
    public string Message { get; }
    public bool RequiresAcknowledgement { get; }

    public RelayCommand CloseCommand { get; }
    public RelayCommand AcknowledgeCommand { get; }

    public ToastViewModel(
        ToastLevel level,
        string title,
        string message,
        bool requiresAcknowledgement,
        Action closeAction)
    {
        Level = level;
        Title = title;
        Message = message;
        RequiresAcknowledgement = requiresAcknowledgement;

        CloseCommand = new RelayCommand(closeAction);
        AcknowledgeCommand = new RelayCommand(closeAction);
    }
}

