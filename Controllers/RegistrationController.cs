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
public class RegistrationController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ISupabaseRealtimeService _supabaseRealtimeService;

    public RegistrationController(
        ApplicationDbContext context,
        INotificationService notificationService,
        ISupabaseRealtimeService supabaseRealtimeService)
    {
        _context = context;
        _notificationService = notificationService;
        _supabaseRealtimeService = supabaseRealtimeService;
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.Student)]
    public async Task<IActionResult> Create(int competitionId)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var currentUser = await _context.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserID == userId.Value);
        var competition = await _context.Competitions.AsNoTracking()
            .FirstOrDefaultAsync(c => c.CompetitionID == competitionId);

        if (currentUser == null || competition == null)
        {
            return NotFound();
        }

        var alreadyRegistered = await _context.Registrations
            .AnyAsync(r => r.UserID == currentUser.UserID && r.CompetitionID == competitionId);

        if (alreadyRegistered)
        {
            TempData["ErrorMessage"] = "You are already registered for this competition.";
            return RedirectToAction("Details", "Competition", new { id = competitionId });
        }

        if (competition.MaxTeamSize > 1)
        {
            // Prefer student.rollnumber/department when present; many accounts only have users + role claim.
            var student = await _context.Students.AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserID == currentUser.UserID);

            var members = new List<TeamMemberFormRow>
            {
                new()
                {
                    Name = currentUser.Name,
                    RollNumber = student?.RollNumber,
                    Department = student?.Department,
                    Email = currentUser.Email
                }
            };
            for (var k = 1; k < competition.MaxTeamSize; k++)
            {
                members.Add(new TeamMemberFormRow());
            }

            var vm = new TeamRegistrationViewModel
            {
                CompetitionID = competitionId,
                CompetitionName = competition.Name,
                TeamName = string.Empty,
                Members = members
            };
            return View("RegisterTeam", vm);
        }

        var registration = new Registration
        {
            UserID = currentUser.UserID,
            CompetitionID = competitionId,
            Type = RegistrationTypes.Individual,
            Status = RegistrationStatuses.Pending,
            RegisteredAt = DateTime.UtcNow
        };

        _context.Registrations.Add(registration);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == "P0001")
        {
            TempData["ErrorMessage"] = pg.MessageText;
            return RedirectToAction("Details", "Competition", new { id = competitionId });
        }

        await _supabaseRealtimeService.PublishSeatUpdateAsync(competition.CompetitionID, 0);

        await _notificationService.CreateAsync(
            currentUser.UserID,
            $"Registration submitted for '{competition.Name}'. Status: {RegistrationStatuses.Pending}.");

        TempData["SuccessMessage"] = "Registration submitted. Awaiting approval and payment verification.";
        return RedirectToAction(nameof(MyRegistrations));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Student)]
    public async Task<IActionResult> RegisterTeam(TeamRegistrationViewModel vm)
    {
        vm.Members ??= new List<TeamMemberFormRow>();

        var userId = User.GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var competition = await _context.Competitions.AsNoTracking()
            .FirstOrDefaultAsync(c => c.CompetitionID == vm.CompetitionID);
        if (competition == null)
        {
            return NotFound();
        }

        if (competition.MaxTeamSize <= 1)
        {
            return RedirectToAction(nameof(Create), new { competitionId = vm.CompetitionID });
        }

        if (vm.Members.Count != competition.MaxTeamSize)
        {
            ModelState.AddModelError(
                string.Empty,
                $"This competition requires exactly {competition.MaxTeamSize} team member(s).");
        }

        var currentUser = await _context.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserID == userId.Value);
        if (currentUser == null)
        {
            return NotFound();
        }

        var alreadyRegistered = await _context.Registrations
            .AnyAsync(r => r.UserID == currentUser.UserID && r.CompetitionID == vm.CompetitionID);
        if (alreadyRegistered)
        {
            TempData["ErrorMessage"] = "You are already registered for this competition.";
            return RedirectToAction("Details", "Competition", new { id = vm.CompetitionID });
        }

        if (vm.Members.Count > 0
            && !string.Equals(
                vm.Members[0].Email.Trim(),
                currentUser.Email,
                StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("Members[0].Email", "Member 1 must be you — use your account email.");
        }

        if (!ModelState.IsValid)
        {
            vm.CompetitionName = competition.Name;
            NormalizeMemberList(vm, competition.MaxTeamSize, currentUser);
            return View("RegisterTeam", vm);
        }

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            var team = new Team
            {
                TeamName = vm.TeamName.Trim(),
                LeaderUserID = userId.Value,
                CompetitionID = vm.CompetitionID
            };
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            foreach (var row in vm.Members)
            {
                _context.TeamMembers.Add(new TeamMember
                {
                    TeamID = team.TeamID,
                    Name = row.Name.Trim(),
                    Email = row.Email.Trim(),
                    RollNumber = string.IsNullOrWhiteSpace(row.RollNumber) ? null : row.RollNumber.Trim(),
                    Department = string.IsNullOrWhiteSpace(row.Department) ? null : row.Department.Trim()
                });
            }

            _context.Registrations.Add(new Registration
            {
                UserID = currentUser.UserID,
                CompetitionID = vm.CompetitionID,
                TeamID = team.TeamID,
                Type = RegistrationTypes.Team,
                Status = RegistrationStatuses.Pending,
                RegisteredAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == "P0001")
        {
            await tx.RollbackAsync();
            _context.ChangeTracker.Clear();
            ModelState.AddModelError(string.Empty, pg.MessageText);
            vm.CompetitionName = competition.Name;
            NormalizeMemberList(vm, competition.MaxTeamSize, currentUser);
            return View("RegisterTeam", vm);
        }
        catch
        {
            await tx.RollbackAsync();
            _context.ChangeTracker.Clear();
            throw;
        }

        await _supabaseRealtimeService.PublishSeatUpdateAsync(competition.CompetitionID, 0);

        await _notificationService.CreateAsync(
            currentUser.UserID,
            $"Team registration submitted for '{competition.Name}'. Status: {RegistrationStatuses.Pending}.");

        TempData["SuccessMessage"] = "Team registration submitted. Awaiting approval and payment verification.";
        return RedirectToAction(nameof(MyRegistrations));
    }

    private static void NormalizeMemberList(
        TeamRegistrationViewModel vm,
        int maxTeamSize,
        User leader)
    {
        while (vm.Members.Count < maxTeamSize)
        {
            vm.Members.Add(new TeamMemberFormRow());
        }

        while (vm.Members.Count > maxTeamSize)
        {
            vm.Members.RemoveAt(vm.Members.Count - 1);
        }

        if (vm.Members.Count > 0
            && string.IsNullOrWhiteSpace(vm.Members[0].Email)
            && !string.IsNullOrWhiteSpace(leader.Email))
        {
            vm.Members[0].Name = leader.Name;
            vm.Members[0].Email = leader.Email;
        }
    }

    [HttpGet]
    public async Task<IActionResult> MyRegistrations()
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var currentUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserID == userId.Value);

        if (currentUser == null)
        {
            return View(new List<MyRegistrationViewModel>());
        }

        var registrations = await _context.Registrations
            .AsNoTracking()
            .Where(r => r.UserID == currentUser.UserID)
            .Include(r => r.Competition)
            .ThenInclude(c => c.Event)
            .Include(r => r.Ticket)
            .Include(r => r.Payment)
            .OrderByDescending(r => r.RegisteredAt)
            .ToListAsync();

        var model = registrations.Select(r => new MyRegistrationViewModel
        {
            RegistrationID = r.RegistrationID,
            CompetitionID = r.CompetitionID,
            CompetitionName = r.Competition.Name,
            CompetitionStartDate = r.Competition.StartDate,
            CompetitionLocation = r.Competition.Location,
            RegistrationType = r.Type,
            RegistrationStatus = r.Status,
            DisplayStatus = ResolveDisplayStatus(r),
            CanUploadPayment = r.Status == RegistrationStatuses.Pending && r.Payment == null
        }).ToList();

        return View(model);
    }

    private static string ResolveDisplayStatus(Registration r)
    {
        if (r.Ticket != null)
        {
            return "Ticket issued";
        }

        return r.Status;
    }
}
