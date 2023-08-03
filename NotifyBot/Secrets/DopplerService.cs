using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace NotifyBot.Secrets;

public class DopplerService : IDopplerService
{
    private Dictionary<string, string> Secrets { get; set; }
    private DateTime? LastFetch { get; set; }

    private static readonly HttpClient Client = new();

    private readonly IConfiguration _configuration;

    private bool IsInvalid
    {
        get
        {
            if (!LastFetch.HasValue) return true;

            var dt = DateTime.UtcNow - LastFetch.Value;

            return dt.Minutes >= 2;
        }
    }

    private string Token { get; }

    public DopplerService(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        Token = _configuration["Doppler:ServiceToken"] ??
                throw new NullReferenceException("Doppler Service Token not found");
        Secrets = new Dictionary<string, string>();
        LastFetch = null;
    }

    public async Task<string> Get(string key)
    {
        if (IsInvalid) await FetchSecrets();

        return Secrets.GetValueOrDefault(key) ?? throw new InvalidDataException(key);
    }

    private async Task FetchSecrets()
    {
        Secrets.Clear();
        LastFetch = null;

        var authHeader = Convert.ToBase64String(Encoding.Default.GetBytes(Token + ":"));
        Client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeader);

        var stream =
            await Client.GetStreamAsync("https://api.doppler.com/v3/configs/config/secrets/download?format=json");
        var secrets = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream);

        if (secrets == null)
            return;

        Secrets = secrets;
        LastFetch = DateTime.UtcNow;
    }
}