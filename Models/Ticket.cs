using System.ComponentModel.DataAnnotations;

namespace EventManagementPortal.Models;

public class Ticket
{
    public int TicketID { get; set; }

    public int RegistrationID { get; set; }

    [Required]
    public string QrCode { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string UniqueCode { get; set; } = string.Empty;

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    public Registration Registration { get; set; } = null!;
}
