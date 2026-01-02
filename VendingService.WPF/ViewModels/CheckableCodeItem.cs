namespace VendingService.WPF.ViewModels;

public sealed class CheckableCodeItem : ObservableObject
{
    private bool _isSelected;

    public string Code { get; }
    public string Name { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public CheckableCodeItem(string code, string name, bool isSelected = false)
    {
        Code = code;
        Name = name;
        _isSelected = isSelected;
    }
}

