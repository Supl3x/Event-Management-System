using System.Security.Claims;
using EventManagementPortal.Data;
using EventManagementPortal.Infrastructure;
using EventManagementPortal.Models;
using EventManagementPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EventManagementPortal.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly ApplicationDbContext _context;

    public DashboardController(IDashboardService dashboardService, ApplicationDbContext context)
    {
        _dashboardService = dashboardService;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var roles = User.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .Distinct()
            .ToList();
        var displayName = User.FindFirstValue(ClaimTypes.GivenName)
            ?? User.Identity?.Name?.Split('@')[0];

        var model = await _dashboardService.BuildDashboardAsync(
            displayName,
            roles,
            User.GetUserId());
        return View(model);
    }

    [Authorize(Roles = AppRoles.Admin)]
    public IActionResult Admin()
    {
        return View();
    }

    [Authorize(Roles = AppRoles.Organizer)]
    public IActionResult Organizer()
    {
        return View();
    }

    [Authorize(Roles = AppRoles.Student)]
    public async Task<IActionResult> Student()
    {
        var userId = User.GetUserId();
        ViewBag.VolunteerRequestEvents = await _context.Events
            .AsNoTracking()
            .OrderBy(e => e.StartDate)
            .Select(e => new { e.EventID, e.Name })
            .ToListAsync();
        if (userId is not null)
        {
            try
            {
                ViewBag.MyVolunteerRequests = await _context.VolunteerEventRequests
                    .AsNoTracking()
                    .Include(r => r.Event)
                    .Where(r => r.StudentID == userId.Value)
                    .OrderByDescending(r => r.RequestedAt)
                    .Select(r => new
                    {
                        r.RequestID,
                        EventName = r.Event.Name,
                        r.Status,
                        r.OrganizerDecision,
                        r.AdminDecision,
                        r.RequestedAt
                    })
                    .ToListAsync();
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
            {
                ViewBag.MyVolunteerRequests = new List<object>();
                TempData["ErrorMessage"] = "Volunteer requests table is missing in database. Apply latest SQL migration and retry.";
            }
        }
        return View();
    }

    [Authorize(Roles = AppRoles.Volunteer)]
    public IActionResult Volunteer()
    {
        return View();
    }
}
