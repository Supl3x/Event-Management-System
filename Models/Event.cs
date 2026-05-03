using System.ComponentModel.DataAnnotations;

namespace EventManagementPortal.Models;

public class Event
{
    public int EventID { get; set; }

    [Required]
    [StringLength(500)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Department { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Location { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Timeline status based on today's date against start/end dates.
    /// </summary>
    public string Status => GetStatus(DateTime.Now);

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    /// <summary>FK to <c>organizer.userid</c> (who created the event).</summary>
    public int CreatedBy { get; set; }

    public OrganizerProfile Creator { get; set; } = null!;
    public ICollection<Competition> Competitions { get; set; } = new List<Competition>();

    public string GetStatus(DateTime now)
    {
        var today = now.Date;
        if (today < StartDate.Date)
        {
            return EventStatuses.Upcoming;
        }

        if (today > EndDate.Date)
        {
            return EventStatuses.Ended;
        }

        return EventStatuses.Live;
    }
}
