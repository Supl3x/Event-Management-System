using EventManagementPortal.Data;
using EventManagementPortal.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagementPortal.Services;

public class TicketService : ITicketService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public TicketService(ApplicationDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    /// <summary>Removes the ticket row (schema has no cancelled state). Optional hard-delete only.</summary>
    public async Task<TicketCancellationResult> CancelTicketAsync(int ticketId, bool hardDelete = false)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Registration)
            .FirstOrDefaultAsync(t => t.TicketID == ticketId);
        if (ticket == null)
        {
            return new TicketCancellationResult
            {
                Success = false,
                Message = "Ticket not found."
            };
        }

        await using var tx = await _context.Database.BeginTransactionAsync();
        var reg = ticket.Registration;
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM competition WHERE competitionid = {reg.CompetitionID} FOR UPDATE");
        var competition = await _context.Competitions
            .FirstOrDefaultAsync(c => c.CompetitionID == reg.CompetitionID);
        if (competition == null)
        {
            await tx.RollbackAsync();
            return new TicketCancellationResult
            {
                Success = false,
                Message = "Competition not found."
            };
        }

        reg.Status = RegistrationStatuses.Cancelled;
        reg.PriorityNumber = null;

        _context.Tickets.Remove(ticket);
        competition.AvailableSeats += 1;

        var promoted = await _context.Registrations
            .Where(r => r.CompetitionID == reg.CompetitionID && r.Status == RegistrationStatuses.Waitlist)
            .OrderBy(r => r.PriorityNumber ?? int.MaxValue)
            .ThenBy(r => r.RegisteredAt)
            .FirstOrDefaultAsync();

        if (promoted != null)
        {
            promoted.Status = RegistrationStatuses.Confirmed;
            promoted.PriorityNumber = null;
            if (competition.AvailableSeats > 0)
            {
                competition.AvailableSeats -= 1;
            }
        }

        await _context.SaveChangesAsync();
        await tx.CommitAsync();

        if (promoted != null)
        {
            await _notificationService.CreateAsync(
                promoted.UserID,
                "You have been moved from waitlist to confirmed for your competition registration.");
        }

        return new TicketCancellationResult
        {
            Success = true,
            Message = "Ticket removed.",
            PromotedRegistrationId = promoted?.RegistrationID
        };
    }
}
