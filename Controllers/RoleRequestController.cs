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
public class RoleRequestController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public RoleRequestController(ApplicationDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Student)]
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Student)]
    public async Task<IActionResult> RequestVolunteerForEvent(int eventId)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var student = await _context.Students
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserID == userId.Value);
        if (student == null)
        {
            TempData["ErrorMessage"] = "Only student accounts can request volunteer access.";
            return RedirectToAction("Student", "Dashboard");
        }

        var evt = await _context.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EventID == eventId);
        if (evt == null)
        {
            TempData["ErrorMessage"] = "Selected event was not found.";
            return RedirectToAction("Student", "Dashboard");
        }

        bool hasPending;
        try
        {
            hasPending = await _context.VolunteerEventRequests.AnyAsync(r =>
                r.EventID == eventId
                && r.StudentID == student.UserID
                && r.Status == VolunteerRequestDecisionStatuses.Pending);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            TempData["ErrorMessage"] = "Volunteer request feature requires latest database migration.";
            return RedirectToAction("Student", "Dashboard");
        }
        if (hasPending)
        {
            TempData["ErrorMessage"] = "You already have a pending volunteer request for this event.";
            return RedirectToAction("Student", "Dashboard");
        }

        var request = new VolunteerEventRequest
        {
            EventID = eventId,
            StudentID = student.UserID,
            Status = VolunteerRequestDecisionStatuses.Pending,
            OrganizerDecision = VolunteerRequestDecisionStatuses.Pending,
            AdminDecision = VolunteerRequestDecisionStatuses.Pending,
            RequestedAt = DateTime.UtcNow
        };
        _context.VolunteerEventRequests.Add(request);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            TempData["ErrorMessage"] = "Volunteer request feature requires latest database migration.";
            return RedirectToAction("Student", "Dashboard");
        }

        await _notificationService.CreateAsync(
            evt.CreatedBy,
            $"{student.User.Name} requested volunteer access for event '{evt.Name}'.");
        var adminIds = await _context.Admins.Select(a => a.UserID).ToListAsync();
        foreach (var adminId in adminIds)
        {
            await _notificationService.CreateAsync(
                adminId,
                $"{student.User.Name} requested volunteer access for event '{evt.Name}'.");
        }

        TempData["SuccessMessage"] = "Volunteer request submitted to both organizer and admin.";
        return RedirectToAction("Student", "Dashboard");
    }

    [Authorize(Roles = AppRoles.Organizer)]
    [HttpGet]
    public async Task<IActionResult> OrganizerVolunteerRequests()
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        List<VolunteerEventRequest> requests;
        try
        {
            requests = await _context.VolunteerEventRequests
                .AsNoTracking()
                .Include(r => r.Event)
                .Include(r => r.Student)
                .ThenInclude(s => s.User)
                .Where(r => r.Event.CreatedBy == userId.Value
                    && r.Status == VolunteerRequestDecisionStatuses.Pending
                    && r.OrganizerDecision == VolunteerRequestDecisionStatuses.Pending)
                .OrderBy(r => r.RequestedAt)
                .ToListAsync();
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            TempData["ErrorMessage"] = "Volunteer request feature requires latest database migration.";
            requests = new List<VolunteerEventRequest>();
        }

        return View(requests);
    }

    [Authorize(Roles = AppRoles.Organizer)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewVolunteerRequestByOrganizer(int requestId, bool approve)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        VolunteerEventRequest? request;
        try
        {
            request = await _context.VolunteerEventRequests
                .Include(r => r.Event)
                .FirstOrDefaultAsync(r => r.RequestID == requestId);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            TempData["ErrorMessage"] = "Volunteer request feature requires latest database migration.";
            return RedirectToAction(nameof(OrganizerVolunteerRequests));
        }
        if (request == null)
        {
            return NotFound();
        }
        if (request.Event.CreatedBy != userId.Value)
        {
            return Forbid();
        }
        if (request.Status != VolunteerRequestDecisionStatuses.Pending)
        {
            TempData["ErrorMessage"] = "This volunteer request was already reviewed.";
            return RedirectToAction(nameof(OrganizerVolunteerRequests));
        }

        request.OrganizerDecision = approve
            ? VolunteerRequestDecisionStatuses.Approved
            : VolunteerRequestDecisionStatuses.Rejected;
        request.OrganizerReviewedBy = userId.Value;
        request.OrganizerReviewedAt = DateTime.UtcNow;

        await ApplyFinalVolunteerDecisionAsync(request);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = approve
            ? "Volunteer request approved by organizer."
            : "Volunteer request rejected by organizer.";
        return RedirectToAction(nameof(OrganizerVolunteerRequests));
    }

    private async Task ApplyFinalVolunteerDecisionAsync(VolunteerEventRequest request)
    {
        if (request.OrganizerDecision == VolunteerRequestDecisionStatuses.Rejected
            || request.AdminDecision == VolunteerRequestDecisionStatuses.Rejected)
        {
            request.Status = VolunteerRequestDecisionStatuses.Rejected;
            await _notificationService.CreateAsync(
                request.StudentID,
                $"Your volunteer request for event #{request.EventID} was rejected.");
            return;
        }

        if (request.OrganizerDecision == VolunteerRequestDecisionStatuses.Approved
            || request.AdminDecision == VolunteerRequestDecisionStatuses.Approved)
        {
            request.Status = VolunteerRequestDecisionStatuses.Approved;
            if (!await _context.Volunteers.AnyAsync(v => v.UserID == request.StudentID))
            {
                _context.Volunteers.Add(new Volunteer { UserID = request.StudentID });
            }

            await _notificationService.CreateAsync(
                request.StudentID,
                $"Your volunteer request for event #{request.EventID} was approved.");
            return;
        }

        request.Status = VolunteerRequestDecisionStatuses.Pending;
        await _notificationService.CreateAsync(
            request.StudentID,
            $"Your volunteer request for event #{request.EventID} is pending review by organizer and admin.");
    }
}
