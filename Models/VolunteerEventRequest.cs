using System.ComponentModel.DataAnnotations;

namespace EventManagementPortal.Models;

public static class VolunteerRequestDecisionStatuses
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
}

public class VolunteerEventRequest
{
    public int RequestID { get; set; }
    public int EventID { get; set; }
    public int StudentID { get; set; }
    public int? OrganizerReviewedBy { get; set; }
    public int? AdminReviewedBy { get; set; }

    [Required]
    [StringLength(20)]
    public string OrganizerDecision { get; set; } = VolunteerRequestDecisionStatuses.Pending;

    [Required]
    [StringLength(20)]
    public string AdminDecision { get; set; } = VolunteerRequestDecisionStatuses.Pending;

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = VolunteerRequestDecisionStatuses.Pending;

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? OrganizerReviewedAt { get; set; }
    public DateTime? AdminReviewedAt { get; set; }

    public Event Event { get; set; } = null!;
    public Student Student { get; set; } = null!;
}
