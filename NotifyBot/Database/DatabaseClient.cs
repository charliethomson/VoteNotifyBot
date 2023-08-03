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

    public async Task<ICollection<User>> FetchAllUsers()
    {
        await InitializeAsync();
        var result = await _client.From<User>().Get();
        return result.Models;
    }

    public async Task<ICollection<Vote>> FetchAllExpiredVotes()
    {
        await InitializeAsync();
        var results = await _client.From<Vote>()
            // Expires < Now => Is expired
            // .Filter(vote => vote.ExpiresAt, Constants.Operator.LessThan, DateTime.UtcNow)
            // .Filter(vote => vote.HasNotified, Constants.Operator.Equals, false)
            .Get();
        Console.WriteLine(results.Models.Count);
        return results.Models;
    }

    public async Task<User?> FetchUser(string userId)
    {
        await InitializeAsync();
        return await _client.From<User>().Filter(user => user.UserId, Constants.Operator.Equals, userId).Single();
    }
}