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
}
