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

    /// <summary>FK to <c>organizer.userid</c> (who created the event).</summary>
    public int CreatedBy { get; set; }

    public OrganizerProfile Creator { get; set; } = null!;
    public ICollection<Competition> Competitions { get; set; } = new List<Competition>();
}
