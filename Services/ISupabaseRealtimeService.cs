namespace EventManagementPortal.Services;

public interface ISupabaseRealtimeService
{
    Task PublishSeatUpdateAsync(int competitionId, int availableSeats);
    Task PublishNotificationAsync(int userId, string message);
}
