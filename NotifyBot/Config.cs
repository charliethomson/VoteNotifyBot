using Newtonsoft.Json;

namespace NotifyBot;

public class Config
{
    [JsonProperty("token")]
    private string? _token { get; set; }
    [JsonProperty("database-url")]
    private string? _databaseUrl { get; set; }
    [JsonProperty("database-pk")]
    private string? _databasePublicKey { get; set; }

    private static Config? _instance = null;

    private static Config Construct()
    {
        _instance = JsonConvert
            .DeserializeObject<Config>(File.ReadAllText(@"C:\Users\c\git\NotifyBot\NotifyBot\config.json"))
            ?? throw new InvalidDataException("shit broke");
        return _instance;
    }

    public static Config Instance => _instance ?? Construct();

    public string Token => _token ?? throw new ArgumentNullException($"{nameof(Config)}:{nameof(_token)}");
    public string DatabaseUrl => _databaseUrl ?? throw new ArgumentNullException($"{nameof(Config)}:{nameof(_databaseUrl)}");
    public string DatabasePublicKey => _databasePublicKey ?? throw new ArgumentNullException($"{nameof(Config)}:{nameof(_databasePublicKey)}");
}