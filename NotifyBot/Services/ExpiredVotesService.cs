using NotifyBot.Database;
using NotifyBot.Database.Models;

namespace NotifyBot.Services;

public class ExpiredVotesService : IExpiredVotesService
{
    private readonly IDatabaseClient _databaseClient;
    private ICollection<Vote> Votes { get; set; } = new List<Vote>();
    private ICollection<User> Users { get; set; } = new List<User>();

    public ICollection<PopulatedVote> GetVotes()
    {
        var users = Users.ToDictionary(user => user.UserId);

        return Votes.Select(vote => new PopulatedVote()
        {
            User = users[vote.UserId],
            Id = vote.Id,
            CreatedAt = vote.CreatedAt,
            ExpiresAt = vote.ExpiresAt,
            NotifiedAt = vote.NotifiedAt
        }).ToList();
    }

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private AsyncTimer? _timer;

    public ExpiredVotesService(IDatabaseClient databaseClient)
    {
        _databaseClient = databaseClient ?? throw new ArgumentNullException(nameof(databaseClient));
    }

    public async Task StartAsync()
    {
        _timer = new AsyncTimer(UpdateVotes, null);
        await _timer.StartAsync(_cancellationTokenSource.Token);
    }

    public async Task StopAsync()
    {
        if (_timer == null) return;
        await _timer.StopAsync(_cancellationTokenSource.Token);
    }

    private async Task UpdateVotes(CancellationToken cancellationToken)
    {
        Votes = await _databaseClient.FetchAllExpiredVotes(cancellationToken);
        Users = await _databaseClient.FetchAllUsers(cancellationToken);
    }

    public async void Dispose()
    {
        await StopAsync();
        _timer?.Dispose();
    }
}