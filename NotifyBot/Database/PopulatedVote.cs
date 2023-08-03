using NotifyBot.Database.Models;

namespace NotifyBot.Database;

public class PopulatedVote
{
    
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? NotifiedAt { get; set; }

    public User User { get; set; }

    public bool HasNotified => NotifiedAt != null;
}