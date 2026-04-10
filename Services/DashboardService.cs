using EventManagementPortal.Data;
using EventManagementPortal.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagementPortal.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _db;

    public DashboardService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardViewModel> BuildDashboardAsync(string? userName, IList<string> roles, int? userId)
    {
        var today = DateTime.UtcNow.Date;

        var upcomingEvents = await _db.Events
            .AsNoTracking()
            .CountAsync(e => e.StartDate >= today);

        var openCompetitions = await _db.Competitions
            .AsNoTracking()
            .CountAsync(c => c.StartDate >= today);

        var myRegs = 0;
        if (userId is not null)
        {
            myRegs = await _db.Registrations
                .AsNoTracking()
                .CountAsync(r => r.UserID == userId.Value);
        }

        return new DashboardViewModel
        {
            UserName = string.IsNullOrWhiteSpace(userName) ? "User" : userName,
            Roles = roles,
            UpcomingEventsCount = upcomingEvents,
            OpenCompetitionsCount = openCompetitions,
            MyRegistrationsCount = myRegs
        };
    }
}
