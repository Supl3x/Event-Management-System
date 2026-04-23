using EventManagementPortal.Data;
using EventManagementPortal.Infrastructure;
using EventManagementPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventManagementPortal.Controllers;

public class EventController : Controller
{
    private readonly ApplicationDbContext _context;

    public EventController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var events = await _context.Events
            .AsNoTracking()
            .Include(e => e.Creator)
            .ThenInclude(c => c.User)
            .OrderBy(e => e.StartDate)
            .ToListAsync();

        return View(events);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var eventItem = await _context.Events
            .AsNoTracking()
            .Include(e => e.Creator)
            .ThenInclude(c => c.User)
            .Include(e => e.Competitions)
            .FirstOrDefaultAsync(e => e.EventID == id);

        if (eventItem == null)
        {
            return NotFound();
        }

        var userId = User.GetUserId();
        var isAdmin = User.IsInRole(AppRoles.Admin);
        ViewBag.CanManageEvent = userId is not null && (isAdmin || eventItem.CreatedBy == userId.Value);
        var statusByCompetition = new Dictionary<int, string>();
        if (userId is not null && eventItem.Competitions.Count > 0)
        {
            var competitionIds = eventItem.Competitions.Select(c => c.CompetitionID).ToList();
            statusByCompetition = await _context.Registrations
                .AsNoTracking()
                .Where(r => r.UserID == userId.Value && competitionIds.Contains(r.CompetitionID))
                .ToDictionaryAsync(r => r.CompetitionID, r => r.Status);
        }
        ViewBag.CompetitionRegistrationStatuses = statusByCompetition;

        if (userId is not null && User.IsInRole(AppRoles.Volunteer))
        {
            var volunteerAssignment = await _context.EventStaffAssignments
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.EventID == eventItem.EventID && x.UserID == userId.Value);
            ViewBag.VolunteerEventRole = volunteerAssignment?.Role;
        }

        return View(eventItem);
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.Organizer)]
    public IActionResult Create()
    {
        return View(new EventCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Organizer)]
    public async Task<IActionResult> Create(EventCreateViewModel vm)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        if (!await _context.Organizers.AnyAsync(o => o.UserID == userId.Value))
        {
            return Forbid();
        }

        if (vm.StartDate is { } sd && vm.EndDate is { } ed && ed < sd)
        {
            ModelState.AddModelError(nameof(vm.EndDate), "End date must be on or after the start date.");
        }

        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var entity = new Event
        {
            Name = vm.Name.Trim(),
            Department = vm.Department.Trim(),
            Location = vm.Location.Trim(),
            StartDate = vm.StartDate!.Value.ToDateTime(TimeOnly.MinValue),
            EndDate = vm.EndDate!.Value.ToDateTime(TimeOnly.MinValue),
            CreatedBy = userId.Value
        };

        _context.Events.Add(entity);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Event \"{entity.Name}\" was created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.OrganizerOrAdmin)]
    public async Task<IActionResult> Delete(int id)
    {
        var actorUserId = User.GetUserId();
        if (actorUserId is null)
        {
            return Challenge();
        }

        var eventEntity = await _context.Events.FirstOrDefaultAsync(e => e.EventID == id);
        if (eventEntity == null)
        {
            TempData["ErrorMessage"] = "Event not found.";
            return RedirectToAction(nameof(Index));
        }

        var isAdmin = User.IsInRole(AppRoles.Admin);
        if (!isAdmin && eventEntity.CreatedBy != actorUserId.Value)
        {
            return Forbid();
        }

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            var competitionIds = await _context.Competitions
                .Where(c => c.EventID == id)
                .Select(c => c.CompetitionID)
                .ToListAsync();

            if (competitionIds.Count > 0)
            {
                var registrations = await _context.Registrations
                    .Where(r => competitionIds.Contains(r.CompetitionID))
                    .ToListAsync();
                _context.Registrations.RemoveRange(registrations);

                var teams = await _context.Teams
                    .Where(t => competitionIds.Contains(t.CompetitionID))
                    .ToListAsync();
                if (teams.Count > 0)
                {
                    var teamIds = teams.Select(t => t.TeamID).ToList();
                    var members = await _context.TeamMembers
                        .Where(m => teamIds.Contains(m.TeamID))
                        .ToListAsync();
                    _context.TeamMembers.RemoveRange(members);
                    _context.Teams.RemoveRange(teams);
                }

                var competitionStaff = await _context.CompetitionStaffAssignments
                    .Where(cs => competitionIds.Contains(cs.CompetitionID))
                    .ToListAsync();
                _context.CompetitionStaffAssignments.RemoveRange(competitionStaff);

                var competitions = await _context.Competitions
                    .Where(c => competitionIds.Contains(c.CompetitionID))
                    .ToListAsync();
                _context.Competitions.RemoveRange(competitions);
            }

            var eventStaff = await _context.EventStaffAssignments
                .Where(es => es.EventID == id)
                .ToListAsync();
            _context.EventStaffAssignments.RemoveRange(eventStaff);

            _context.Events.Remove(eventEntity);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        TempData["SuccessMessage"] = $"Event \"{eventEntity.Name}\" was deleted.";
        return RedirectToAction(nameof(Index));
    }
}
