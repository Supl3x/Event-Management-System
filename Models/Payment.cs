using System.ComponentModel.DataAnnotations;

namespace EventManagementPortal.Models;

public class Payment
{
    public int PaymentID { get; set; }

    public int RegistrationID { get; set; }

    [Required]
    [StringLength(1000)]
    public string Screenshot { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = PaymentStatuses.Pending;

    public int? VerifiedBy { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public DateTime? VerifiedAt { get; set; }

    public Registration Registration { get; set; } = null!;
    public User? Verifier { get; set; }
}
