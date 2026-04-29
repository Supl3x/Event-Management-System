namespace EventManagementPortal.Models;

public class RoleManagementPageViewModel
{
    public List<OrganizerRoleRequest> PendingRequests { get; set; } = new();
    public List<VolunteerEventRequest> PendingVolunteerRequestsForAdmin { get; set; } = new();
    public List<UserRoleStatusViewModel> Users { get; set; } = new();
}

public class UserRoleStatusViewModel
{
    public int UserID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsStudent { get; set; }
    public bool IsOrganizer { get; set; }
    public bool IsVolunteer { get; set; }
    public bool IsAdmin { get; set; }
}

public class StaffAssignmentsViewModel
{
    public int ParentId { get; set; }
    public string ParentName { get; set; } = string.Empty;
    public string ParentType { get; set; } = string.Empty;
    public List<StaffAssignmentRowViewModel> Assigned { get; set; } = new();
    public List<StaffAssignmentRowViewModel> Available { get; set; } = new();
}

public class StaffAssignmentRowViewModel
{
    public int UserID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Volunteer";
}
