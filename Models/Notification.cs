using System.ComponentModel.DataAnnotations;

namespace EventManagementPortal.Models;

public class Notification
{
    public int NotificationID { get; set; }
    public int UserID { get; set; }

    [Required]
    [StringLength(500)]
    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }

    public User User { get; set; } = null!;
}
