using NotifyBot.Database;
using Serilog;

namespace NotifyBot.Services;

public class NotifyService : INotifyService
{
    private AsyncTimer? _timer;
    private readonly IExpiredVotesService _expiredVotesService;
    private readonly ILogger _logger;
    private Func<PopulatedVote, CancellationToken, Task>? _notifyUser;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public NotifyService(IExpiredVotesService expiredVotesService, ILogger logger)
    {
        _expiredVotesService = expiredVotesService ?? throw new ArgumentNullException(nameof(expiredVotesService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }


    public async Task StartAsync(Func<PopulatedVote, CancellationToken, Task>? notifyUser)
    {
        _notifyUser = notifyUser ?? throw new ArgumentNullException(nameof(notifyUser));

        _timer = new AsyncTimer(NotifyUsers, TimeSpan.FromSeconds(5));
        await _timer.StartAsync(_cancellationTokenSource.Token);
    }

    public async Task StopAsync()
    {
        if (_timer == null) return;
        await _timer.StopAsync(_cancellationTokenSource.Token);
    }

    private async Task NotifyUsers(CancellationToken cancellationToken)
    {
        if (_notifyUser == null) return; // Unreachable
        var votes = _expiredVotesService.GetVotes();
        if (votes.Count == 0) return;

        foreach (var vote in votes)
        {
            _logger.Information(
                $"Got new expired vote, notifying {vote.User.UserName} (UserId={vote.User.UserId};CreatedAt={vote.CreatedAt};ExpiresAt({vote.ExpiresAt})");
            await _notifyUser(vote, cancellationToken);
        }
    }
}