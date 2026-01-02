using System.Linq;
using System.Windows.Threading;
using VendingService.WPF.ViewModels;

namespace VendingService.WPF.Services.Toasts;

public sealed class ToastService : ObservableObject
{
    private sealed record ToastRequest(
        DateTime CreatedAt,
        ToastLevel Level,
        string Title,
        string Message,
        TimeSpan Duration,
        bool RequiresAcknowledgement);

    private readonly List<ToastRequest> _queue = new();
    private ToastViewModel? _currentToast;
    private DispatcherTimer? _timer;

    public ToastViewModel? CurrentToast
    {
        get => _currentToast;
        private set
        {
            if (SetProperty(ref _currentToast, value))
            {
                RaisePropertyChanged(nameof(HasToast));
            }
        }
    }

    public bool HasToast => CurrentToast != null;

    public void Enqueue(ToastLevel level, string title, string message, TimeSpan duration, bool requiresAcknowledgement = false)
    {
        lock (_queue)
        {
            _queue.Add(new ToastRequest(
                CreatedAt: DateTime.Now,
                Level: level,
                Title: title,
                Message: message,
                Duration: duration,
                RequiresAcknowledgement: requiresAcknowledgement));
        }

        TryShowNext();
    }

    private void TryShowNext()
    {
        if (CurrentToast != null)
        {
            return;
        }

        ToastRequest? next;

        lock (_queue)
        {
            next = _queue
                .OrderBy(x => x.Level)
                .ThenBy(x => x.CreatedAt)
                .FirstOrDefault();

            if (next != null)
            {
                _queue.Remove(next);
            }
        }

        if (next == null)
        {
            return;
        }

        CurrentToast = new ToastViewModel(
            next.Level,
            next.Title,
            next.Message,
            next.RequiresAcknowledgement,
            closeAction: CloseCurrent);

        StartTimer(next.Duration);
    }

    private void StartTimer(TimeSpan duration)
    {
        StopTimer();

        _timer = new DispatcherTimer
        {
            Interval = duration
        };

        _timer.Tick += TimerOnTick;
        _timer.Start();
    }

    private void TimerOnTick(object? sender, EventArgs e)
    {
        CloseCurrent();
    }

    public void CloseCurrent()
    {
        StopTimer();
        CurrentToast = null;
        TryShowNext();
    }

    private void StopTimer()
    {
        if (_timer == null)
        {
            return;
        }

        _timer.Stop();
        _timer.Tick -= TimerOnTick;
        _timer = null;
    }
}
