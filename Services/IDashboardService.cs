using EventManagementPortal.Models;

namespace EventManagementPortal.Services;

public interface IDashboardService
{
    Task<DashboardViewModel> BuildDashboardAsync(string? userName, IList<string> roles, int? userId);
}
