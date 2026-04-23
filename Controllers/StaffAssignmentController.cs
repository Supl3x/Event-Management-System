using EventManagementPortal.Data;
using EventManagementPortal.Infrastructure;
using EventManagementPortal.Models;
using EventManagementPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventManagementPortal.Controllers;

[Authorize]
public class StaffAssignmentController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public StaffAssignmentController(ApplicationDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.OrganizerOrAdmin)]
    public async Task<IActionResult> Event(int id)
    {
        var actorUserId = User.GetUserId();
        if (actorUserId is null)
        {
            return Challenge();
        }

        var ev = await _context.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventID == id);
        if (ev == null)
        {
            return NotFound();
        }

        var isAdmin = await _context.Admins.AnyAsync(a => a.UserID == actorUserId.Value);
        if (!isAdmin && ev.CreatedBy != actorUserId.Value)
        {
            return Forbid();
        }

        var assigned = await _context.EventStaffAssignments
            .AsNoTracking()
            .Where(x => x.EventID == id)
            .Join(_context.Users, es => es.UserID, u => u.UserID, (es, u) => new StaffAssignmentRowViewModel
            {
                UserID = u.UserID,
                Name = u.Name,
                Email = u.Email,
                Role = es.Role
            })
            .OrderBy(x => x.Name)
            .ToListAsync();

        var assignedUserIds = assigned.Select(a => a.UserID).ToHashSet();
        var available = await _context.Volunteers
            .AsNoTracking()
            .Join(_context.Users, v => v.UserID, u => u.UserID, (v, u) => new StaffAssignmentRowViewModel
            {
                UserID = u.UserID,
                Name = u.Name,
                Email = u.Email
            })
            .Where(v => !assignedUserIds.Contains(v.UserID))
            .OrderBy(v => v.Name)
            .ToListAsync();

        return View("Manage", new StaffAssignmentsViewModel
        {
            ParentId = ev.EventID,
            ParentName = ev.Name,
            ParentType = "Event",
            Assigned = assigned,
            Available = available
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Volunteer)]
    public async Task<IActionResult> RequestEventVolunteer(int eventId)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        if (!await _context.Volunteers.AnyAsync(v => v.UserID == userId.Value))
        {
            TempData["ErrorMessage"] = "Only volunteer accounts can request verification access.";
            return RedirectToAction("Details", "Event", new { id = eventId });
        }

        var ev = await _context.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventID == eventId);
        if (ev == null)
        {
            return NotFound();
        }

        var existing = await _context.EventStaffAssignments
            .FirstOrDefaultAsync(x => x.EventID == eventId && x.UserID == userId.Value);
        if (existing == null)
        {
            _context.EventStaffAssignments.Add(new EventStaff
            {
                EventID = eventId,
                UserID = userId.Value,
                Role = "PendingApproval"
            });
            await _context.SaveChangesAsync();

            await _notificationService.CreateAsync(
                ev.CreatedBy,
                $"Volunteer access request received for event \"{ev.Name}\".");
            TempData["SuccessMessage"] = "Your request was submitted to the organizer for approval.";
        }
        else if (string.Equals(existing.Role, "PendingApproval", StringComparison.OrdinalIgnoreCase))
        {
            TempData["ErrorMessage"] = "You already have a pending request for this event.";
        }
        else
        {
            TempData["SuccessMessage"] = "You already have approved access for this event.";
        }

        return RedirectToAction("Details", "Event", new { id = eventId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.OrganizerOrAdmin)]
    public async Task<IActionResult> ApproveEventVolunteer(int eventId, int userId)
    {
        var actorUserId = User.GetUserId();
        if (actorUserId is null)
        {
            return Challenge();
        }

        var ev = await _context.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventID == eventId);
        if (ev == null)
        {
            return NotFound();
        }

        var isAdmin = await _context.Admins.AnyAsync(a => a.UserID == actorUserId.Value);
        if (!isAdmin && ev.CreatedBy != actorUserId.Value)
        {
            return Forbid();
        }

        var row = await _context.EventStaffAssignments
            .FirstOrDefaultAsync(x => x.EventID == eventId && x.UserID == userId);
        if (row == null)
        {
            return NotFound();
        }

        row.Role = "Volunteer";
        await _context.SaveChangesAsync();
        await _notificationService.CreateAsync(
            userId,
            $"Your volunteer verification access was approved for event \"{ev.Name}\".");

        return RedirectToAction(nameof(Event), new { id = eventId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.OrganizerOrAdmin)]
    public async Task<IActionResult> AddEventVolunteer(int eventId, int userId, string? role)
    {
        var actorUserId = User.GetUserId();
        if (actorUserId is null)
        {
            return Challenge();
        }

        var ev = await _context.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventID == eventId);
        if (ev == null)
        {
            return NotFound();
        }
        var isAdmin = await _context.Admins.AnyAsync(a => a.UserID == actorUserId.Value);
        if (!isAdmin && ev.CreatedBy != actorUserId.Value)
        {
            return Forbid();
        }

        if (!await _context.EventStaffAssignments.AnyAsync(x => x.EventID == eventId && x.UserID == userId))
        {
            _context.EventStaffAssignments.Add(new EventStaff
            {
                EventID = eventId,
                UserID = userId,
                Role = string.IsNullOrWhiteSpace(role) ? "Volunteer" : role.Trim()
            });
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Event), new { id = eventId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.OrganizerOrAdmin)]
    public async Task<IActionResult> RemoveEventVolunteer(int eventId, int userId)
    {
        var actorUserId = User.GetUserId();
        if (actorUserId is null)
        {
            return Challenge();
        }

        var ev = await _context.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventID == eventId);
        if (ev == null)
        {
            return NotFound();
        }
        var isAdmin = await _context.Admins.AnyAsync(a => a.UserID == actorUserId.Value);
        if (!isAdmin && ev.CreatedBy != actorUserId.Value)
        {
            return Forbid();
        }

        var row = await _context.EventStaffAssignments.FirstOrDefaultAsync(x => x.EventID == eventId && x.UserID == userId);
        if (row != null)
        {
            _context.EventStaffAssignments.Remove(row);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Event), new { id = eventId });
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.OrganizerOrAdmin)]
    public async Task<IActionResult> Competition(int id)
    {
        var actorUserId = User.GetUserId();
        if (actorUserId is null)
        {
            return Challenge();
        }

        var comp = await _context.Competitions.AsNoTracking().FirstOrDefaultAsync(c => c.CompetitionID == id);
        if (comp == null)
        {
            return NotFound();
        }

        var eventOwnerId = await _context.Events
            .Where(e => e.EventID == comp.EventID)
            .Select(e => e.CreatedBy)
            .FirstOrDefaultAsync();
        var isAdmin = await _context.Admins.AnyAsync(a => a.UserID == actorUserId.Value);
        if (!isAdmin && eventOwnerId != actorUserId.Value)
        {
            return Forbid();
        }

        var assigned = await _context.CompetitionStaffAssignments
            .AsNoTracking()
            .Where(x => x.CompetitionID == id)
            .Join(_context.Users, cs => cs.UserID, u => u.UserID, (cs, u) => new StaffAssignmentRowViewModel
            {
                UserID = u.UserID,
                Name = u.Name,
                Email = u.Email,
                Role = cs.Role
            })
            .OrderBy(x => x.Name)
            .ToListAsync();

        var assignedUserIds = assigned.Select(a => a.UserID).ToHashSet();
        var available = await _context.Volunteers
            .AsNoTracking()
            .Join(_context.Users, v => v.UserID, u => u.UserID, (v, u) => new StaffAssignmentRowViewModel
            {
                UserID = u.UserID,
                Name = u.Name,
                Email = u.Email
            })
            .Where(v => !assignedUserIds.Contains(v.UserID))
            .OrderBy(v => v.Name)
            .ToListAsync();

        return View("Manage", new StaffAssignmentsViewModel
        {
            ParentId = comp.CompetitionID,
            ParentName = comp.Name,
            ParentType = "Competition",
            Assigned = assigned,
            Available = available
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.OrganizerOrAdmin)]
    public async Task<IActionResult> AddCompetitionVolunteer(int competitionId, int userId, string? role)
    {
        var actorUserId = User.GetUserId();
        if (actorUserId is null)
        {
            return Challenge();
        }

        var parent = await _context.Competitions
            .Join(_context.Events, c => c.EventID, e => e.EventID, (c, e) => new { c.CompetitionID, e.CreatedBy })
            .FirstOrDefaultAsync(x => x.CompetitionID == competitionId);
        if (parent == null)
        {
            return NotFound();
        }
        var isAdmin = await _context.Admins.AnyAsync(a => a.UserID == actorUserId.Value);
        if (!isAdmin && parent.CreatedBy != actorUserId.Value)
        {
            return Forbid();
        }

        if (!await _context.CompetitionStaffAssignments.AnyAsync(x => x.CompetitionID == competitionId && x.UserID == userId))
        {
            _context.CompetitionStaffAssignments.Add(new CompetitionStaff
            {
                CompetitionID = competitionId,
                UserID = userId,
                Role = string.IsNullOrWhiteSpace(role) ? "Volunteer" : role.Trim()
            });
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Competition), new { id = competitionId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.OrganizerOrAdmin)]
    public async Task<IActionResult> RemoveCompetitionVolunteer(int competitionId, int userId)
    {
        var actorUserId = User.GetUserId();
        if (actorUserId is null)
        {
            return Challenge();
        }

        var parent = await _context.Competitions
            .Join(_context.Events, c => c.EventID, e => e.EventID, (c, e) => new { c.CompetitionID, e.CreatedBy })
            .FirstOrDefaultAsync(x => x.CompetitionID == competitionId);
        if (parent == null)
        {
            return NotFound();
        }
        var isAdmin = await _context.Admins.AnyAsync(a => a.UserID == actorUserId.Value);
        if (!isAdmin && parent.CreatedBy != actorUserId.Value)
        {
            return Forbid();
        }

        var row = await _context.CompetitionStaffAssignments
            .FirstOrDefaultAsync(x => x.CompetitionID == competitionId && x.UserID == userId);
        if (row != null)
        {
            _context.CompetitionStaffAssignments.Remove(row);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Competition), new { id = competitionId });
    }
}
