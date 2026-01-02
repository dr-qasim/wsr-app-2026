namespace VendingService.WPF.ViewModels;

public sealed class CheckableLookupItem : ObservableObject
{
    private bool _isSelected;

    public int Id { get; }
    public string Name { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public CheckableLookupItem(int id, string name, bool isSelected = false)
    {
        Id = id;
        Name = name;
        _isSelected = isSelected;
    }
}

