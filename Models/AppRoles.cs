namespace EventManagementPortal.Models;

public static class AppRoles
{
    public const string Student = "Student";
    public const string Admin = "Admin";
    public const string Organizer = "Organizer";
    public const string Volunteer = "Volunteer";

    /// <summary>ASP.NET Core Authorize(Roles): comma means OR.</summary>
    public const string OrganizerOrVolunteer = $"{Organizer},{Volunteer}";
}
