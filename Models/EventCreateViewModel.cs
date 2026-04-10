using System.ComponentModel.DataAnnotations;

namespace EventManagementPortal.Models;

public class EventCreateViewModel
{
    [Required]
    [StringLength(500)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Department { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Start date is required.")]
    [DataType(DataType.Date)]
    public DateOnly? StartDate { get; set; }

    [Required(ErrorMessage = "End date is required.")]
    [DataType(DataType.Date)]
    public DateOnly? EndDate { get; set; }
}
