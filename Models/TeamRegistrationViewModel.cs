using System.ComponentModel.DataAnnotations;

namespace EventManagementPortal.Models;

public class TeamMemberFormRow
{
    [Required(ErrorMessage = "Member name is required.")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    public string? RollNumber { get; set; }

    [StringLength(100)]
    public string? Department { get; set; }

    [Required(ErrorMessage = "Email is required.")]
    [StringLength(256)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class TeamRegistrationViewModel
{
    public int CompetitionID { get; set; }

    /// <summary>Display only; not posted for security decisions.</summary>
    public string CompetitionName { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string TeamName { get; set; } = string.Empty;

    public List<TeamMemberFormRow> Members { get; set; } = new();
}
