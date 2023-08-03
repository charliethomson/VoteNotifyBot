using Microsoft.Extensions.Configuration;
using NotifyBot.Database.Models;
using NotifyBot.Secrets;
using Postgrest;
using Supabase;
using Client = Supabase.Client;

namespace NotifyBot.Database;

public class DatabaseClient : IDatabaseClient
{
    private bool _initialized = false;
    private Client? _client = null;
    private readonly IDopplerService _dopplerService;

    public DatabaseClient(IDopplerService dopplerService)
    {
        _dopplerService = dopplerService ?? throw new ArgumentNullException(nameof(dopplerService));
    }

    public async Task InitializeAsync()
    {
        if (_client != null && _initialized) return;
        var url = await _dopplerService.Get(DopplerSecrets.DatabaseUrl) ?? throw new InvalidDataException(DopplerSecrets.DatabaseUrl);
        var key = await _dopplerService.Get(DopplerSecrets.DatabasePublicKey) ?? throw new InvalidDataException(DopplerSecrets.DatabasePublicKey);

        var options = new SupabaseOptions()
        {
            AutoConnectRealtime = true
        };

        _client = new Client(url, key, options);
        await _client.InitializeAsync();

        _initialized = true;
    }

    public async Task<ICollection<User>> FetchAllUsers(CancellationToken cancellationToken = default)
    {
        await InitializeAsync();
        var result = await _client!.From<User>().Get(cancellationToken);
        return result.Models;
    }

    public async Task<ICollection<Vote>> FetchAllVotes(CancellationToken cancellationToken = default)
    {
        await InitializeAsync();
        var result = await _client!.From<Vote>().Get(cancellationToken);
        return result.Models;
    }

    public async Task<ICollection<Vote>> FetchAllExpiredVotes(CancellationToken cancellationToken = default)
    {
        await InitializeAsync();
        var results = await FetchAllVotes(cancellationToken);

        return results.GroupBy(result => result.UserId)
            .Select(group => group.OrderByDescending(vote => vote.ExpiresAt).First()).Where(vote => !vote.HasNotified).Where(vote => vote.ExpiresAt < DateTime.UtcNow).ToList();
    }

    public async Task<User?> FetchUser(string userId, CancellationToken cancellationToken = default)
    {
        await InitializeAsync();
        return await _client!.From<User>().Filter(user => user.UserId, Constants.Operator.Equals, userId)
            .Single(cancellationToken);
    }

    public async Task SetNotificationTime(int voteId, CancellationToken cancellationToken = default)
    {
        await InitializeAsync();
        await _client.From<Vote>().Filter(vote => vote.Id, Constants.Operator.Equals, voteId)
            .Set(vote => vote.NotifiedAt, DateTime.UtcNow).Update(null, cancellationToken);
    }
}