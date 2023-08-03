using Postgrest.Attributes;
using Postgrest.Models;

namespace NotifyBot.Database.Models;

[Table("votes")]
public class Vote : BaseModel
{
    [PrimaryKey("id")] public int Id { get; set; }
    [Column("uid")] public string UserId { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; }
    [Column("expires_at")] public DateTime ExpiresAt { get; set; }
    [Column("notified_at")] public DateTime? NotifiedAt { get; set; }

    public bool HasNotified => NotifiedAt != null;
}