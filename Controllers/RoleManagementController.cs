using EventManagementPortal.Data;
using EventManagementPortal.Infrastructure;
using EventManagementPortal.Models;
using EventManagementPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventManagementPortal.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class RoleManagementController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public RoleManagementController(ApplicationDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var pendingRequests = await _context.OrganizerRoleRequests
            .AsNoTracking()
            .Where(r => r.Status == "Pending")
            .Include(r => r.Student)
            .ThenInclude(s => s.User)
            .OrderBy(r => r.RequestedAt)
            .ToListAsync();

        var userRows = await _context.Users
            .AsNoTracking()
            .Select(u => new UserRoleStatusViewModel
            {
                UserID = u.UserID,
                Name = u.Name,
                Email = u.Email,
                IsStudent = _context.Students.Any(s => s.UserID == u.UserID),
                IsOrganizer = _context.Organizers.Any(o => o.UserID == u.UserID),
                IsVolunteer = _context.Volunteers.Any(v => v.UserID == u.UserID),
                IsAdmin = _context.Admins.Any(a => a.UserID == u.UserID)
            })
            .OrderBy(u => u.Name)
            .ToListAsync();

        return View(new RoleManagementPageViewModel
        {
            PendingRequests = pendingRequests,
            Users = userRows
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewOrganizerRequest(int requestId, bool approve)
    {
        var adminUserId = User.GetUserId();
        if (adminUserId is null)
        {
            return Challenge();
        }
        var request = await _context.OrganizerRoleRequests.FirstOrDefaultAsync(r => r.RequestID == requestId);
        if (request == null)
        {
            return NotFound();
        }

        request.Status = approve ? "Approved" : "Rejected";
        request.ApprovedBy = adminUserId.Value;
        request.ReviewedAt = DateTime.UtcNow;

        if (approve && !await _context.Organizers.AnyAsync(o => o.UserID == request.StudentID))
        {
            _context.Organizers.Add(new OrganizerProfile { UserID = request.StudentID });
        }

        await _context.SaveChangesAsync();
        var decisionMessage = approve
            ? "Your organizer role request was approved."
            : "Your organizer role request was rejected.";
        await _notificationService.CreateAsync(request.StudentID, decisionMessage);

        TempData["SuccessMessage"] = approve ? "Organizer request approved." : "Organizer request rejected.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PromoteOrganizer(int userId)
    {
        if (!await _context.Users.AnyAsync(u => u.UserID == userId))
        {
            return NotFound();
        }

        if (!await _context.Organizers.AnyAsync(o => o.UserID == userId))
        {
            _context.Organizers.Add(new OrganizerProfile { UserID = userId });
            await _context.SaveChangesAsync();
            await _notificationService.CreateAsync(userId, "Your role was updated: you are now an Organizer.");
        }

        TempData["SuccessMessage"] = "User promoted to organizer.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PromoteVolunteer(int userId)
    {
        if (!await _context.Users.AnyAsync(u => u.UserID == userId))
        {
            return NotFound();
        }

        if (!await _context.Volunteers.AnyAsync(v => v.UserID == userId))
        {
            _context.Volunteers.Add(new Volunteer { UserID = userId });
            await _context.SaveChangesAsync();
            await _notificationService.CreateAsync(userId, "Your role was updated: you are now a Volunteer.");
        }

        TempData["SuccessMessage"] = "User promoted to volunteer.";
        return RedirectToAction(nameof(Index));
    }
}
