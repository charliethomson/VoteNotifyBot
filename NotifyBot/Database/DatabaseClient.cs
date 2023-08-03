using NotifyBot.Database.Models;
using Postgrest;
using Supabase;

namespace NotifyBot.Database;

public class DatabaseClient
{
    private bool _initialized = false;
    private Supabase.Client _client;
    private static DatabaseClient? _instance = null;
    private static DatabaseClient Instance = _instance ?? Construct();

    private static DatabaseClient Construct()
    {
        _instance = new DatabaseClient();
        return _instance;
    }

    public DatabaseClient()
    {
        if (_instance != null)
        {
            _client = _instance._client;
            _initialized = _instance._initialized;
            return;
        }

        var config = Config.Instance;

        var url = config.DatabaseUrl;
        var key = config.DatabasePublicKey;

        var options = new SupabaseOptions()
        {
            AutoConnectRealtime = true
        };

        _client = new Supabase.Client(url, key, options);
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        await _client.InitializeAsync();

        _initialized = true;
    }

    public async Task<ICollection<User>> FetchAllUsers(CancellationToken cancellationToken = default)
    {
        await InitializeAsync();
        var result = await _client.From<User>().Get(cancellationToken);
        return result.Models;
    }

    public async Task<ICollection<Vote>> FetchAllVotes(CancellationToken cancellationToken = default)
    {
        await InitializeAsync();
        var result = await _client.From<Vote>().Get(cancellationToken);
        return result.Models;
    }

    public async Task<ICollection<Vote>> FetchAllExpiredVotes(CancellationToken cancellationToken = default)
    {
        await InitializeAsync();
        var results = await FetchAllVotes(cancellationToken);

        return results.GroupBy(result => result.UserId)
            .Select(group => group.OrderByDescending(vote => vote.ExpiresAt).First()).Where(vote => !vote.HasNotified).ToList();
    }

    public async Task<User?> FetchUser(string userId, CancellationToken cancellationToken = default)
    {
        await InitializeAsync();
        return await _client.From<User>().Filter(user => user.UserId, Constants.Operator.Equals, userId)
            .Single(cancellationToken);
    }

    public async Task SetNotificationTime(int voteId, CancellationToken cancellationToken = default)
    {
        await InitializeAsync();
        await _client.From<Vote>().Filter(vote => vote.Id, Constants.Operator.Equals, voteId)
            .Set(vote => vote.NotifiedAt, DateTime.UtcNow).Update(null, cancellationToken);
    }
}