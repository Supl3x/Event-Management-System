using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventManagementPortal.Models;

public class LoginViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = AppRoles.Student;

    [StringLength(50)]
    public string? RollNumber { get; set; }

    [StringLength(100)]
    public string? Department { get; set; }

    public string? ReturnUrl { get; set; }

    [BindNever]
    public IEnumerable<SelectListItem> RoleOptions =>
    [
        new(AppRoles.Student, AppRoles.Student),
        new(AppRoles.Admin, AppRoles.Admin),
        new(AppRoles.Organizer, AppRoles.Organizer),
        new(AppRoles.Volunteer, AppRoles.Volunteer)
    ];
}
