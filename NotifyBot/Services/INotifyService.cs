using NotifyBot.Database;

namespace NotifyBot.Services;

public interface INotifyService
{
    Task StartAsync(Func<PopulatedVote, CancellationToken, Task>? notifyUser);

    Task StopAsync();
}