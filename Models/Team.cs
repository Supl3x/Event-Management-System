using System.ComponentModel.DataAnnotations;

namespace EventManagementPortal.Models;

public class Team
{
    public int TeamID { get; set; }

    [Required]
    [StringLength(200)]
    public string TeamName { get; set; } = string.Empty;

    public int LeaderUserID { get; set; }

    public int CompetitionID { get; set; }

    public User Leader { get; set; } = null!;
    public Competition Competition { get; set; } = null!;
    public ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
}
