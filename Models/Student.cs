using System.ComponentModel.DataAnnotations;

namespace EventManagementPortal.Models;

public class Student
{
    public int UserID { get; set; }

    [Required]
    [StringLength(100)]
    public string RollNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Department { get; set; } = string.Empty;

    public User User { get; set; } = null!;
}
