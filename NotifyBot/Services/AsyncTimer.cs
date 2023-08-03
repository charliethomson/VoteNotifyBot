namespace NotifyBot.Services;

public class AsyncTimer : IDisposable
{
    private Timer? _timer;
    private Task? _executingTask = null;
    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    private readonly TimeSpan _interval;
    private readonly Func<CancellationToken, Task> _callback;

    public AsyncTimer(Func<CancellationToken, Task> callback, TimeSpan? interval)
    {
        _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        _interval = interval ?? TimeSpan.FromSeconds(5);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(ExecuteTask, null, _interval, TimeSpan.FromMilliseconds(-1));
        return Task.CompletedTask;
    }

    private void ExecuteTask(object? state)
    {
        _timer?.Change(Timeout.Infinite, 0);
        _executingTask = ExecuteTaskAsync(_cancellationTokenSource.Token);
    }

    private async Task ExecuteTaskAsync(CancellationToken cancellationToken)
    {
        await _callback(cancellationToken);
        _timer?.Change(_interval, TimeSpan.FromMilliseconds(-1));
    }


    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);

        // Stop called without start
        if (_executingTask == null)
        {
            return;
        }

        try
        {
            // Signal cancellation to the executing method
            _cancellationTokenSource.Cancel();
        }
        finally
        {
            // Wait until the task completes or the stop token triggers
            await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _timer?.Dispose();
    }
}