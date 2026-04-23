using EventManagementPortal.Data;
using EventManagementPortal.Infrastructure;
using EventManagementPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventManagementPortal.Controllers;

[Authorize(Roles = AppRoles.Student)]
public class RoleRequestController : Controller
{
    private readonly ApplicationDbContext _context;

    public RoleRequestController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestOrganizer()
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var isStudent = await _context.Students.AnyAsync(s => s.UserID == userId.Value);
        if (!isStudent)
        {
            TempData["ErrorMessage"] = "Only student accounts can request organizer access.";
            return RedirectToAction("Index", "Dashboard");
        }

        var hasPending = await _context.OrganizerRoleRequests
            .AnyAsync(r => r.StudentID == userId.Value && r.Status == "Pending");
        if (hasPending)
        {
            TempData["ErrorMessage"] = "You already have a pending organizer request.";
            return RedirectToAction("Student", "Dashboard");
        }

        _context.OrganizerRoleRequests.Add(new OrganizerRoleRequest
        {
            StudentID = userId.Value,
            Status = "Pending",
            RequestedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Organizer role request submitted to admins.";
        return RedirectToAction("Student", "Dashboard");
    }
}
