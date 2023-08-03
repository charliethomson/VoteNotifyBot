using NotifyBot.Database;

namespace NotifyBot.Services;

public interface INotifyService : IDisposable
{
    Task StartAsync(Func<PopulatedVote, CancellationToken, Task>? notifyUser);

    Task StopAsync();
}