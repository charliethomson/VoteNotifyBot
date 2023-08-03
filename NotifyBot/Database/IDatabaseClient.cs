using NotifyBot.Database.Models;

namespace NotifyBot.Database;

public interface IDatabaseClient
{
    Task InitializeAsync();
    Task<ICollection<User>> FetchAllUsers(CancellationToken cancellationToken = default);
    Task<ICollection<Vote>> FetchAllVotes(CancellationToken cancellationToken = default);
    Task<ICollection<Vote>> FetchAllExpiredVotes(CancellationToken cancellationToken = default);
    Task<User?> FetchUser(string userId, CancellationToken cancellationToken = default);
    Task SetNotificationTime(int voteId, CancellationToken cancellationToken = default);
    Task<PopulatedVote?> GetNewestVoteForUserId(string userId, CancellationToken cancellationToken = default);
    Task<User?> GetUserByUsername(string userName, CancellationToken cancellationToken = default);
}