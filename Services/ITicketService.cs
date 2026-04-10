namespace EventManagementPortal.Services;

public interface ITicketService
{
    Task<TicketCancellationResult> CancelTicketAsync(int ticketId, bool hardDelete = false);
}

public class TicketCancellationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? PromotedRegistrationId { get; set; }
}
