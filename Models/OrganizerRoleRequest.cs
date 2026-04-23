using System.ComponentModel.DataAnnotations;

namespace EventManagementPortal.Models;

public class OrganizerRoleRequest
{
    public int RequestID { get; set; }
    public int StudentID { get; set; }
    public int? ApprovedBy { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Pending";

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }

    public Student Student { get; set; } = null!;
    public Admin? Reviewer { get; set; }
}
