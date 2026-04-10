using EventManagementPortal.Data;
using Microsoft.EntityFrameworkCore;

namespace EventManagementPortal.Services;

public class TicketService : ITicketService
{
    private readonly ApplicationDbContext _context;

    public TicketService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>Removes the ticket row (schema has no cancelled state). Optional hard-delete only.</summary>
    public async Task<TicketCancellationResult> CancelTicketAsync(int ticketId, bool hardDelete = false)
    {
        var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.TicketID == ticketId);
        if (ticket == null)
        {
            return new TicketCancellationResult
            {
                Success = false,
                Message = "Ticket not found."
            };
        }

        _context.Tickets.Remove(ticket);
        await _context.SaveChangesAsync();

        return new TicketCancellationResult
        {
            Success = true,
            Message = "Ticket removed.",
            PromotedRegistrationId = null
        };
    }
}
