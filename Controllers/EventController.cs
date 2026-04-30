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
    private readonly ILogger<EventController> _logger;

    public EventController(ApplicationDbContext context, ILogger<EventController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        string? department,
        string? status,
        DateOnly? startFrom,
        DateOnly? startTo,
        int page = 1,
        int pageSize = 9)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 6, 24);
        var now = DateTime.Now;

        var query = _context.Events
            .AsNoTracking()
            .Include(e => e.Creator)
            .ThenInclude(c => c.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(e =>
                e.Name.Contains(term) ||
                e.Department.Contains(term) ||
                e.Location.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(department))
        {
            query = query.Where(e => e.Department == department);
        }

        if (startFrom is { } sf)
        {
            var fromDate = sf.ToDateTime(TimeOnly.MinValue);
            query = query.Where(e => e.StartDate >= fromDate);
        }

        if (startTo is { } st)
        {
            var toDate = st.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(e => e.StartDate <= toDate);
        }

        var baseEvents = await query
            .OrderBy(e => e.StartDate)
            .ToListAsync();

        var cards = baseEvents
            .Select(e => new EventCardViewModel
            {
                EventID = e.EventID,
                Name = e.Name,
                Department = e.Department,
                Location = e.Location,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                OrganizerName = e.Creator?.User?.Name ?? "Admin",
                CreatedBy = e.CreatedBy,
                Status = e.GetStatus(now),
                CountdownText = GetCountdownText(e, now)
            });

        if (!string.IsNullOrWhiteSpace(status))
        {
            cards = cards.Where(c => c.Status == status);
        }

        var filteredCards = cards.ToList();
        var totalItems = filteredCards.Count;
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
        page = Math.Min(page, totalPages);
        var pagedCards = filteredCards
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var vm = new EventIndexViewModel
        {
            Events = pagedCards,
            Departments = await _context.Events
                .AsNoTracking()
                .Select(e => e.Department)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync(),
            Search = search,
            Department = department,
            Status = status,
            StartFrom = startFrom,
            StartTo = startTo,
            Page = page,
            TotalPages = totalPages,
            TotalItems = totalItems,
            PageSize = pageSize
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var now = DateTime.Now;
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
        var canManageEvent = userId is not null && (isAdmin || eventItem.CreatedBy == userId.Value);
        var statusByCompetition = new Dictionary<int, string>();
        if (userId is not null && eventItem.Competitions.Count > 0)
        {
            var competitionIds = eventItem.Competitions.Select(c => c.CompetitionID).ToList();
            statusByCompetition = await _context.Registrations
                .AsNoTracking()
                .Where(r => r.UserID == userId.Value && competitionIds.Contains(r.CompetitionID))
                .ToDictionaryAsync(r => r.CompetitionID, r => r.Status);
        }
        string? volunteerEventRole = null;

        if (userId is not null && User.IsInRole(AppRoles.Volunteer))
        {
            var volunteerAssignment = await _context.EventStaffAssignments
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.EventID == eventItem.EventID && x.UserID == userId.Value);
            volunteerEventRole = volunteerAssignment?.Role;
        }

        var computedStatus = eventItem.GetStatus(now);
        var vm = new EventDetailsViewModel
        {
            Event = eventItem,
            CompetitionRegistrationStatuses = statusByCompetition,
            VolunteerEventRole = volunteerEventRole,
            CanManageEvent = canManageEvent,
            EventStatus = computedStatus,
            CountdownText = GetCountdownText(eventItem, now),
            IsRegistrationClosed = computedStatus == EventStatuses.Ended,
            RegistrationClosedReason = computedStatus == EventStatuses.Ended
                ? "Registrations are closed because this event has already ended."
                : null
        };

        return View(vm);
    }

    [HttpGet]
    [Authorize(Policy = "OrganizerOnly")]
    public IActionResult Create()
    {
        return View(new EventCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "OrganizerOnly")]
    public async Task<IActionResult> Create(EventCreateViewModel vm)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            _logger.LogWarning("Event create denied: missing user id.");
            return Challenge();
        }

        if (!await _context.Organizers.AnyAsync(o => o.UserID == userId.Value))
        {
            _logger.LogWarning("Event create forbidden for user {UserId}: organizer profile missing.", userId.Value);
            return Forbid();
        }

        if (vm.StartDate is { } sd && vm.EndDate is { } ed && ed < sd)
        {
            ModelState.AddModelError(nameof(vm.EndDate), "End date must be on or after the start date.");
        }

        if (vm.StartDate is { } startDate)
        {
            var minimumStartDate = DateOnly.FromDateTime(DateTime.Now).AddDays(1);
            if (startDate < minimumStartDate)
            {
                ModelState.AddModelError(nameof(vm.StartDate), "Start date must be at least 1 day after today.");
            }
        }

        if (!ModelState.IsValid)
        {
            _logger.LogInformation(
                "Event create validation failed for user {UserId}. Reasons: {Errors}",
                userId.Value,
                string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
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

        try
        {
            _context.Events.Add(entity);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Event create failed for user {UserId}. EventName={EventName}", userId.Value, vm.Name);
            TempData["ErrorMessage"] = "Unable to create event at the moment. Please try again.";
            return View(vm);
        }

        TempData["SuccessMessage"] = $"Event \"{entity.Name}\" was created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "OrganizerOrAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        var actorUserId = User.GetUserId();
        if (actorUserId is null)
        {
            _logger.LogWarning("Event delete denied for event {EventId}: missing user id.", id);
            return Challenge();
        }

        var eventEntity = await _context.Events.FirstOrDefaultAsync(e => e.EventID == id);
        if (eventEntity == null)
        {
            _logger.LogInformation("Event delete failed for user {UserId}: event {EventId} not found.", actorUserId.Value, id);
            TempData["ErrorMessage"] = "Event not found.";
            return RedirectToAction(nameof(Index));
        }

        var isAdmin = User.IsInRole(AppRoles.Admin);
        if (!isAdmin && eventEntity.CreatedBy != actorUserId.Value)
        {
            _logger.LogWarning(
                "Event delete forbidden. User {UserId} attempted deleting event {EventId} owned by {OwnerUserId}.",
                actorUserId.Value,
                id,
                eventEntity.CreatedBy);
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
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            _logger.LogError(ex, "Event delete failed for user {UserId}, event {EventId}.", actorUserId.Value, id);
            TempData["ErrorMessage"] = "Unable to delete event right now. Please try again.";
            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = $"Event \"{eventEntity.Name}\" was deleted.";
        return RedirectToAction(nameof(Index));
    }

    private static string? GetCountdownText(Event ev, DateTime now)
    {
        var days = (ev.StartDate.Date - now.Date).Days;
        if (days > 1)
        {
            return $"Starts in {days} days";
        }

        if (days == 1)
        {
            return "Starts tomorrow";
        }

        return null;
    }
}
