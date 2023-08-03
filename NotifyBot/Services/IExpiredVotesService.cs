using NotifyBot.Database;

namespace NotifyBot.Services;

public interface IExpiredVotesService
{
    public ICollection<PopulatedVote> GetVotes();
    public Task StartAsync();

    public Task StopAsync();
}