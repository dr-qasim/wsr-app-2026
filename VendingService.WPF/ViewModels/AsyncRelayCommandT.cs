using System.Windows.Input;

namespace VendingService.WPF.ViewModels;

public sealed class AsyncRelayCommand<T>(Func<T?, Task> executeAsync, Func<T?, bool>? canExecute = null) : ICommand
{
    private bool _isExecuting;

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        if (_isExecuting)
        {
            return false;
        }

        var value = parameter is null ? default : (T?)parameter;
        return canExecute?.Invoke(value) ?? true;
    }

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        var value = parameter is null ? default : (T?)parameter;

        _isExecuting = true;
        RaiseCanExecuteChanged();

        try
        {
            await executeAsync(value);
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

