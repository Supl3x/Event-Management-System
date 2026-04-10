using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace EventManagementPortal.Models;

public class PaymentCreateViewModel
{
    [Required]
    public int RegistrationID { get; set; }

    [Required]
    [Display(Name = "Payment Screenshot")]
    public IFormFile Screenshot { get; set; } = null!;
}
