using System.ComponentModel.DataAnnotations;

namespace EventManagementPortal.Models;

public class TeamMember
{
    public int MemberID { get; set; }

    public int TeamID { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    public string? RollNumber { get; set; }

    [StringLength(100)]
    public string? Department { get; set; }

    [Required]
    [StringLength(256)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public Team Team { get; set; } = null!;
}
