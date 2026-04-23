using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventManagementPortal.Models;

public class Competition
{
    public int CompetitionID { get; set; }

    public int EventID { get; set; }

    [Required]
    [StringLength(500)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    [StringLength(500)]
    public string Location { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "date")]
    public DateTime StartDate { get; set; }

    [Required]
    [Column(TypeName = "date")]
    public DateTime EndDate { get; set; }

    [Range(1, 5)]
    public int MaxTeamSize { get; set; } = 1;

    [Column(TypeName = "numeric(12,2)")]
    public decimal EntryFee { get; set; }

    [Range(0, int.MaxValue)]
    public int AvailableSeats { get; set; } = 100;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    public Event Event { get; set; } = null!;
    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
    public ICollection<Team> Teams { get; set; } = new List<Team>();
}
