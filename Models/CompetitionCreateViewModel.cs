using System.ComponentModel.DataAnnotations;

namespace EventManagementPortal.Models;

public class CompetitionCreateViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Select an event.")]
    public int EventID { get; set; }

    [Required]
    [StringLength(500)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    [StringLength(500)]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Start date is required.")]
    [DataType(DataType.Date)]
    public DateOnly? StartDate { get; set; }

    [Required(ErrorMessage = "End date is required.")]
    [DataType(DataType.Date)]
    public DateOnly? EndDate { get; set; }

    [Range(1, 5)]
    public int MaxTeamSize { get; set; } = 1;

    public decimal EntryFee { get; set; }
}
