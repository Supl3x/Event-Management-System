namespace EventManagementPortal.Models;

public class OrganizerProfile
{
    public int UserID { get; set; }
    public User User { get; set; } = null!;
    public ICollection<Event> EventsCreated { get; set; } = new List<Event>();
}
