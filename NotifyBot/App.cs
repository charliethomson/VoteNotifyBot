using Discord;
using Discord.WebSocket;
using NotifyBot.Database.Models;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Channels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NotifyBot.Database;
using NotifyBot.Secrets;
using NotifyBot.Services;
using Serilog;

namespace NotifyBot;

public class App : IHostedService
{
    private readonly IDopplerService _dopplerService;
    private readonly IDatabaseClient _databaseClient;
    private readonly IExpiredVotesService _expiredVotesService;

    private readonly INotifyService _notifyService;
    private readonly ILogger _logger;

    private DiscordSocketClient _discordClient;

    private Dictionary<string, ulong> _userChannels = new();

    public App(IDopplerService dopplerService, IDatabaseClient databaseClient, IExpiredVotesService expiredVotesService,
        INotifyService notifyService, ILogger logger)
    {
        _dopplerService = dopplerService ?? throw new ArgumentNullException(nameof(dopplerService));
        _databaseClient = databaseClient ?? throw new ArgumentNullException(nameof(databaseClient));
        _expiredVotesService = expiredVotesService ?? throw new ArgumentNullException(nameof(expiredVotesService));
        _notifyService = notifyService ?? throw new ArgumentNullException(nameof(notifyService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private async Task<string> Token()
    {
        return await _dopplerService.Get(DopplerSecrets.DiscordToken);
    }

    private async Task<ulong> BotUserId()
    {
        var userId = await _dopplerService.Get(DopplerSecrets.BotUserId);
        return ulong.Parse(userId);
    }

    private SocketGuild Guild => _discordClient.GetGuild(897241042724732968);

    private async Task<SocketGuildUser?> GetBotUser()
    {
        var botUserId = await BotUserId();
        return Guild.Users.FirstOrDefault(user => user.IsBot && user.Id == botUserId);
    }

    private async Task<SocketGuildUser?> GetUser(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _databaseClient.FetchUser(userId, cancellationToken);
        return user == null ? null : Guild.Users.FirstOrDefault(u => u.Username == user.UserName);
    }

    private async Task<ulong> CreateChannelAsync(SocketGuildUser discordUser)
    {
        if (discordUser == null) throw new ArgumentNullException(nameof(discordUser));

        var channel = Guild.TextChannels.FirstOrDefault(channel => channel.Name == discordUser.DisplayName);
        if (channel != null) return channel.Id;

        var createdChannel = await Guild.CreateTextChannelAsync(discordUser.DisplayName);
        await createdChannel.AddPermissionOverwriteAsync(Guild.EveryoneRole,
            new OverwritePermissions(viewChannel: PermValue.Deny));
        await createdChannel.AddPermissionOverwriteAsync(discordUser,
            new OverwritePermissions(viewChannel: PermValue.Allow, sendMessages: PermValue.Allow));
        await createdChannel.AddPermissionOverwriteAsync(await GetBotUser(),
            new OverwritePermissions());

        await createdChannel.SendMessageAsync(
            $"👋 Hello {discordUser.Mention}! I will mention you in this channel to remind you to vote 😊");

        return createdChannel.Id;
    }


    private Task OnLog(LogMessage message)
    {
        switch (message.Severity)
        {
            case LogSeverity.Critical:
                _logger.Fatal(message.ToString());
                break;
            case LogSeverity.Debug:
                _logger.Debug(message.ToString());
                break;
            case LogSeverity.Error:
                _logger.Error(message.ToString());
                break;
            case LogSeverity.Info:
                _logger.Information(message.ToString());
                break;
            case LogSeverity.Warning:
                _logger.Warning(message.ToString());
                break;
            case LogSeverity.Verbose:
                _logger.Verbose(message.ToString());
                break;
            default:
                throw new ArgumentOutOfRangeException(message.Severity.ToString());
        }

        return Task.CompletedTask;
    }

    private async Task OnReady()
    {
        await _discordClient.DownloadUsersAsync(new[] { Guild });
        var users = await _databaseClient.FetchAllUsers();
        foreach (var user in users)
        {
            var discordUser = Guild.Users.FirstOrDefault(discordUser => discordUser.Username == user.UserName);
            if (discordUser == null) continue;
            var channel = await CreateChannelAsync(discordUser);
            _userChannels.Add(user.UserId, channel);
        }
    }

    public async Task OnUserJoined(SocketGuildUser user)
    {
        if (_userChannels.ContainsValue(user.Id)) return;

        var users = await _databaseClient.FetchAllUsers();

        var userEntry = users.FirstOrDefault(u => u.UserName == user.Username);
        if (userEntry == null) return;


        var channel = await CreateChannelAsync(user);
        _userChannels.Add(userEntry.UserId, channel);
    }

    private async Task NotifyUser(PopulatedVote vote, CancellationToken cancellationToken)
    {
        var userId = vote.User.UserId;

        if (!_userChannels.ContainsKey(userId))
        {
            _logger.Error($"Failed to notify. Missing UserChannel for UserId={userId}");
            return;
        }

        var channelId = _userChannels[userId];
        var channel = Guild.GetTextChannel(channelId);
        var discordUser = await GetUser(userId, cancellationToken);
        if (discordUser == null)
        {
            _logger.Error($"Failed to notify. Missing DB user for UserId={userId}");
            return;
        }

        var embed = new EmbedBuilder
            {  Description = "[Vote](https://v.ndr.gg)", Color = new Color(16315349) };

        await channel.SendMessageAsync(
            $"👋 {discordUser.Mention}\n" +
            $"(Last voted @ {vote.CreatedAt.ToUniversalTime()} UTC)\n" +
            $"(https://v.ndr.gg)",
            embed: embed.Build());

        await _databaseClient.SetNotificationTime(vote.Id, cancellationToken);
    }

    private void AddEventListeners()
    {
        _discordClient.Log += OnLog;
        _discordClient.Ready += OnReady;
        _discordClient.UserJoined += OnUserJoined;
    }

    private void RemoveEventListeners()
    {
        _discordClient.Log -= OnLog;
        _discordClient.Ready -= OnReady;
        _discordClient.UserJoined -= OnUserJoined;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _databaseClient.InitializeAsync();
        await _expiredVotesService.StartAsync();
        await _notifyService.StartAsync(NotifyUser);

        _discordClient = new DiscordSocketClient(new DiscordSocketConfig()
        {
            AlwaysDownloadUsers = true,
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers |
                             GatewayIntents.GuildMessages ^ GatewayIntents.GuildScheduledEvents ^
                             GatewayIntents.GuildInvites
        });

        AddEventListeners();

        await _discordClient.LoginAsync(TokenType.Bot, await Token());
        await _discordClient.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        RemoveEventListeners();
        await _notifyService.StopAsync();
        await _expiredVotesService.StopAsync();
        await _discordClient.StopAsync();
    }
}