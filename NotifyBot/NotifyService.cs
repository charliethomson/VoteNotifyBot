namespace NotifyBot;

public class NotifyService
{
    private AsyncTimer? _timer;
    private readonly ExpiredVotesService _expiredVotesService;
    private readonly Func<string, Task> _notifyUser;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public NotifyService(ExpiredVotesService expiredVotesService, Func<string, Task> notifyUser)
    {
        _expiredVotesService = expiredVotesService ?? throw new ArgumentNullException(nameof(expiredVotesService));
        _notifyUser = notifyUser ?? throw new ArgumentNullException(nameof(notifyUser));
    }
    

    public async Task StartAsync()
    {
        _timer = new AsyncTimer(NotifyUsers, TimeSpan.FromSeconds(30));
        await _timer.StartAsync(_cancellationTokenSource.Token);
    }

    public async Task StopAsync()
    {
        if (_timer == null) return;
        await _timer.StopAsync(_cancellationTokenSource.Token);
    }
    public async void Dispose()
    {
        await StopAsync();
        _timer?.Dispose();
    }
    private async Task NotifyUsers()
    {
        var votes = _expiredVotesService.Votes;
        if (votes.Count == 0) return;
        
        foreach (var vote in votes)
        {
            await _notifyUser(vote.User.UserId);
        }
        
    }
}