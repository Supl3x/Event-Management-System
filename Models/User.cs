using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EventManagementPortal.Models;

[Index(nameof(Email), IsUnique = true)]
public class User
{
    public int UserID { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(50)]
    public string Phone { get; set; } = string.Empty;

    /// <summary>Optional app password (BCrypt). Add column: <c>ALTER TABLE users ADD COLUMN passwordhash varchar(500);</c> if missing.</summary>
    [StringLength(500)]
    public string? PasswordHash { get; set; }

    public Student? Student { get; set; }
    public Admin? AdminProfile { get; set; }
    public OrganizerProfile? OrganizerProfile { get; set; }
    public Volunteer? VolunteerProfile { get; set; }

    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
    public ICollection<Payment> PaymentsVerified { get; set; } = new List<Payment>();
}
