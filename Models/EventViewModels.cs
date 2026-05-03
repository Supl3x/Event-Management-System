namespace EventManagementPortal.Models;

public class EventCardViewModel
{
    public int EventID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string OrganizerName { get; set; } = "Admin";
    public int CreatedBy { get; set; }
    public string Status { get; set; } = EventStatuses.Upcoming;
    public string? CountdownText { get; set; }
}

public class EventIndexViewModel
{
    public IReadOnlyList<EventCardViewModel> Events { get; set; } = Array.Empty<EventCardViewModel>();
    public IReadOnlyList<string> Departments { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Statuses { get; set; } =
        new[] { EventStatuses.Upcoming, EventStatuses.Live, EventStatuses.Ended };
    public string? Search { get; set; }
    public string? Department { get; set; }
    public string? Status { get; set; }
    public DateOnly? StartFrom { get; set; }
    public DateOnly? StartTo { get; set; }
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
    public int PageSize { get; set; }
}

public class EventDetailsViewModel
{
    public Event Event { get; set; } = null!;
    public IDictionary<int, string> CompetitionRegistrationStatuses { get; set; } = new Dictionary<int, string>();
    public string? VolunteerEventRole { get; set; }
    public bool CanManageEvent { get; set; }
    public string EventStatus { get; set; } = EventStatuses.Upcoming;
    public string? CountdownText { get; set; }
    public bool IsRegistrationClosed { get; set; }
    public string? RegistrationClosedReason { get; set; }
}
