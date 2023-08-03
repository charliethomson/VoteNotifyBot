using Postgrest.Attributes;
using Postgrest.Models;

namespace NotifyBot.Database.Models;

[Table("users")]
public class User : BaseModel
{
    [PrimaryKey("uid")]
    public string UserId { get; set; }
    [Column("createdAt")]
    public DateTime CreatedAt { get; set; }
    [Column("src")]
    public string Source { get; set; }
    [Column("fullName")]
    public string UserName { get; set; }
}