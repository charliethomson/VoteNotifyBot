using NotifyBot.Database;
using NotifyBot.Database.Models;

namespace NotifyBot;

public class ExpiredVotesService : IDisposable
{
    private readonly DatabaseClient _databaseClient;
    private ICollection<Vote> _votes { get; set; } = new List<Vote>();
    private ICollection<User> Users { get; set; } = new List<User>();

    public ICollection<PopulatedVote> Votes
    {
        get
        {
            var users = Users.ToDictionary(user => user.UserId);

            return _votes.Select(vote => new PopulatedVote()
            {
                User = users[vote.UserId],
                Id = vote.Id,
                CreatedAt = vote.CreatedAt,
                ExpiresAt = vote.ExpiresAt,
                NotifiedAt = vote.NotifiedAt
            }).ToList();
        }
    }

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private AsyncTimer? _timer;

    public ExpiredVotesService(DatabaseClient databaseClient)
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

    private async Task UpdateVotes()
    {
        _votes = await _databaseClient.FetchAllExpiredVotes();
        Users = await _databaseClient.FetchAllUsers();
    }

    public async void Dispose()
    {
        await StopAsync();
        _timer?.Dispose();
    }
}