using System.ComponentModel.DataAnnotations;

namespace EventManagementPortal.Models;

public class CompetitionStaff
{
    public int CompetitionID { get; set; }
    public int UserID { get; set; }

    [Required]
    [StringLength(100)]
    public string Role { get; set; } = "Volunteer";

    public Competition Competition { get; set; } = null!;
    public Volunteer Volunteer { get; set; } = null!;
}
