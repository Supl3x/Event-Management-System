namespace EventManagementPortal.Models;

public class Admin
{
    public int UserID { get; set; }
    public User User { get; set; } = null!;
}
