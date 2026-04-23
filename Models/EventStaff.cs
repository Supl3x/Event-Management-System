using System.ComponentModel.DataAnnotations;

namespace EventManagementPortal.Models;

public class EventStaff
{
    public int EventID { get; set; }
    public int UserID { get; set; }

    [Required]
    [StringLength(100)]
    public string Role { get; set; } = "Volunteer";

    public Event Event { get; set; } = null!;
    public Volunteer Volunteer { get; set; } = null!;
}
