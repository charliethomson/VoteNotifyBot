using Discord;
using Discord.WebSocket;
using NotifyBot.Database.Models;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Channels;
using NotifyBot.Database;

namespace NotifyBot;

public class Program
{
    // TODO: DI


    private string Token => Config.Instance.Token;

    private DiscordSocketClient _discordClient;
    private DatabaseClient _databaseClient;
    private ExpiredVotesService _expiredVotesService;
    private NotifyService _notifyService;

    private Dictionary<string, ulong> UserChannels = new();

    public static Task Main(string[] args) => new Program().MainAsync();

    private const ulong BotUserId = 1135173281889136721;

    private SocketGuild Guild => _discordClient.GetGuild(897241042724732968);

    public SocketGuildUser? GetBotUser()
    {
        return Guild.Users.FirstOrDefault(user => user.IsBot && user.Id == BotUserId);
    }

    public async Task<SocketGuildUser?> GetUser(string userId)
    {
        var user = await _databaseClient.FetchUser(userId);
        return user == null ? null : Guild.Users.FirstOrDefault(u => u.Username == user.UserName);
    }

    private Task Log(LogMessage message)
    {
        Console.WriteLine(message.ToString());
        return Task.CompletedTask;
    }

    public async Task<ulong> CreateChannelAsync(SocketGuildUser discordUser)
    {
        if (discordUser == null) throw new ArgumentNullException(nameof(discordUser));

        var channel = Guild.TextChannels.FirstOrDefault(channel => channel.Name == discordUser.DisplayName);
        if (channel != null) return channel.Id;

        var createdChannel = await Guild.CreateTextChannelAsync(discordUser.DisplayName);
        await createdChannel.AddPermissionOverwriteAsync(Guild.EveryoneRole,
            new OverwritePermissions(viewChannel: PermValue.Deny));
        await createdChannel.AddPermissionOverwriteAsync(discordUser,
            new OverwritePermissions(viewChannel: PermValue.Allow, sendMessages: PermValue.Allow));
        await createdChannel.AddPermissionOverwriteAsync(GetBotUser(),
            new OverwritePermissions());

        await createdChannel.SendMessageAsync(
            $"👋 Hello {discordUser.Mention}! I will mention you in this channel to remind you to vote 😊");

        return createdChannel.Id;
    }


    private async Task Ready()
    {
        await _discordClient.DownloadUsersAsync(new[] { Guild });
        var users = await _databaseClient.FetchAllUsers();
        foreach (var user in users)
        {
            var discordUser = Guild.Users.FirstOrDefault(discordUser => discordUser.Username == user.UserName);
            if (discordUser == null) continue;
            var channel = await CreateChannelAsync(discordUser);
            UserChannels.Add(user.UserId, channel);
        }

        foreach (var socketTextChannel in Guild.TextChannels)
        {
            Console.WriteLine(socketTextChannel.Name);
        }

    }

    public async Task UserJoined(SocketGuildUser user)
    {
        if (UserChannels.ContainsValue(user.Id)) return;

        var users = await _databaseClient.FetchAllUsers();

        var userEntry = users.FirstOrDefault(u => u.UserName == user.Username);
        if (userEntry == null) return;


        var channel = await CreateChannelAsync(user);
        UserChannels.Add(userEntry.UserId, channel);
    }

    private async Task NotifyUser(string userId)
    {
        if (!UserChannels.ContainsKey(userId))
        {
            Console.WriteLine($"ERROR: Failed to notify. Missing UserChannel for UserId={userId}");
            return;
        }

        var channelId = UserChannels[userId];
        var channel = Guild.GetTextChannel(channelId);
        var discordUser = await GetUser(userId);
        if (discordUser == null)
        {
            Console.WriteLine($"FATAL: Failed to notify. Missing DB user for UserId={userId}");
            return;
        }

        await channel.SendMessageAsync($"{discordUser.Mention}! Time to vote! :)");

    }
    public async Task MainAsync()
    {
        _databaseClient = new DatabaseClient();

        await _databaseClient.InitializeAsync();

        _expiredVotesService = new ExpiredVotesService(_databaseClient);
        await _expiredVotesService.StartAsync();

        _notifyService = new NotifyService(_expiredVotesService, NotifyUser);
        await _notifyService.StartAsync();
        
        _discordClient = new DiscordSocketClient(new DiscordSocketConfig()
        {
            AlwaysDownloadUsers = true,
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers | GatewayIntents.GuildMessages
        });

        _discordClient.Log += Log;
        _discordClient.Ready += Ready;
        _discordClient.UserJoined += UserJoined;

        await _discordClient.LoginAsync(TokenType.Bot, Token);
        await _discordClient.StartAsync();

        await Task.Delay(-1);

        await _notifyService.StopAsync();
        await _expiredVotesService.StopAsync();
        await _discordClient.StopAsync();
    }

}