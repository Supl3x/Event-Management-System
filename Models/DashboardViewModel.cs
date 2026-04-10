namespace EventManagementPortal.Models;

public class DashboardViewModel
{
    public string UserName { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = new List<string>();
    public int UpcomingEventsCount { get; set; }
    public int MyRegistrationsCount { get; set; }
    public int OpenCompetitionsCount { get; set; }
}
