using EventManagementPortal.Data;
using EventManagementPortal.Infrastructure;
using EventManagementPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EventManagementPortal.Controllers;

public class CompetitionController : Controller
{
    private readonly ApplicationDbContext _context;

    public CompetitionController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var competitions = await _context.Competitions
            .AsNoTracking()
            .Include(c => c.Event)
            .OrderBy(c => c.StartDate)
            .ToListAsync();

        return View(competitions);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var competition = await _context.Competitions
            .AsNoTracking()
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CompetitionID == id);

        if (competition == null)
        {
            return NotFound();
        }

        return View(competition);
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.OrganizerOrAdmin)]
    public async Task<IActionResult> Create()
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var isAdmin = await _context.Admins.AnyAsync(a => a.UserID == userId.Value);
        await PopulateEventsDropDownAsync(userId.Value, isAdmin);
        return View(new CompetitionCreateViewModel { MaxTeamSize = 1, EntryFee = 0, AvailableSeats = 100 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.OrganizerOrAdmin)]
    public async Task<IActionResult> Create(CompetitionCreateViewModel vm)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Challenge();
        }
        var isOrganizer = await _context.Organizers.AnyAsync(o => o.UserID == userId.Value);
        var isAdmin = await _context.Admins.AnyAsync(a => a.UserID == userId.Value);
        if (!isOrganizer && !isAdmin)
        {
            return Forbid();
        }

        Event? parentEvent = null;
        if (vm.EventID > 0)
        {
            parentEvent = await _context.Events.AsNoTracking()
                .FirstOrDefaultAsync(e => e.EventID == vm.EventID);
            if (parentEvent == null)
            {
                ModelState.AddModelError(nameof(vm.EventID), "Select a valid event.");
            }
            else if (!isAdmin && parentEvent.CreatedBy != userId.Value)
            {
                ModelState.AddModelError(nameof(vm.EventID), "You can only create competitions for events you created.");
            }
        }
        else
        {
            ModelState.AddModelError(nameof(vm.EventID), "Select a valid event.");
        }

        if (vm.StartDate is { } sd && vm.EndDate is { } ed && ed < sd)
        {
            ModelState.AddModelError(nameof(vm.EndDate), "End date must be on or after the start date.");
        }

        if (parentEvent != null && vm.StartDate is { } compStart && vm.EndDate is { } compEnd)
        {
            var evStart = DateOnly.FromDateTime(parentEvent.StartDate.Date);
            var evEnd = DateOnly.FromDateTime(parentEvent.EndDate.Date);
            if (compStart < evStart || compEnd > evEnd)
            {
                ModelState.AddModelError(
                    nameof(vm.StartDate),
                    $"Competition dates must fall within this event’s window: {evStart:yyyy-MM-dd} to {evEnd:yyyy-MM-dd} (inclusive).");
            }
        }

        if (vm.EntryFee < 0)
        {
            ModelState.AddModelError(nameof(vm.EntryFee), "Entry fee cannot be negative.");
        }
        if (vm.AvailableSeats < 0)
        {
            ModelState.AddModelError(nameof(vm.AvailableSeats), "Available seats cannot be negative.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateEventsDropDownAsync(userId.Value, isAdmin, vm.EventID);
            return View(vm);
        }

        var entity = new Competition
        {
            EventID = vm.EventID,
            Name = vm.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim(),
            Location = vm.Location.Trim(),
            StartDate = vm.StartDate!.Value.ToDateTime(TimeOnly.MinValue),
            EndDate = vm.EndDate!.Value.ToDateTime(TimeOnly.MinValue),
            MaxTeamSize = vm.MaxTeamSize,
            EntryFee = vm.EntryFee,
            AvailableSeats = vm.AvailableSeats
        };

        _context.Competitions.Add(entity);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg
            && (pg.SqlState == "P0001" || pg.SqlState == PostgresErrorCodes.CheckViolation))
        {
            ModelState.AddModelError(
                string.Empty,
                string.IsNullOrWhiteSpace(pg.MessageText)
                    ? "The database rejected these values (e.g. dates outside the parent event)."
                    : pg.MessageText);
            await PopulateEventsDropDownAsync(userId.Value, isAdmin, vm.EventID);
            _context.Entry(entity).State = EntityState.Detached;
            return View(vm);
        }

        TempData["SuccessMessage"] = $"Competition \"{entity.Name}\" was created.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateEventsDropDownAsync(int userId, bool isAdmin, int? selectedEventId = null)
    {
        var eventsQuery = _context.Events.AsNoTracking();
        if (!isAdmin)
        {
            eventsQuery = eventsQuery.Where(e => e.CreatedBy == userId);
        }

        var events = await eventsQuery
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .Select(e => new
            {
                e.EventID,
                Label = $"{e.Name} ({e.StartDate:yyyy-MM-dd} – {e.EndDate:yyyy-MM-dd})"
            })
            .ToListAsync();

        ViewBag.EventOptions = new SelectList(events, "EventID", "Label", selectedEventId);
    }
}
