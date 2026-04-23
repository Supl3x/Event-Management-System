using System.Security.Claims;
using EventManagementPortal.Infrastructure;
using EventManagementPortal.Models;
using EventManagementPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagementPortal.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
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
    public IActionResult Student()
    {
        return View();
    }

    [Authorize(Roles = AppRoles.Volunteer)]
    public IActionResult Volunteer()
    {
        return View();
    }
}
