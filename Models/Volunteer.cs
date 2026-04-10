namespace EventManagementPortal.Models;

public class Volunteer
{
    public int UserID { get; set; }
    public User User { get; set; } = null!;
}
