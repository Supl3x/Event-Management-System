using System.ComponentModel.DataAnnotations;

namespace EventManagementPortal.Models;

public class Registration
{
    public int RegistrationID { get; set; }

    public int CompetitionID { get; set; }

    public int UserID { get; set; }

    public int? TeamID { get; set; }

    [Required]
    [StringLength(50)]
    public string Type { get; set; } = RegistrationTypes.Individual;

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = RegistrationStatuses.Pending;

    public int? PriorityNumber { get; set; }

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    public User User { get; set; } = null!;
    public Competition Competition { get; set; } = null!;
    public Team? Team { get; set; }
    public Ticket? Ticket { get; set; }
    public Payment? Payment { get; set; }
}
