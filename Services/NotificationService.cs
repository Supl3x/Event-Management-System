namespace EventManagementPortal.Services;

/// <summary>No <c>notification</c> table in the DB schema; only pushes realtime messages.</summary>
public class NotificationService : INotificationService
{
    private readonly ISupabaseRealtimeService _supabaseRealtimeService;

    public NotificationService(ISupabaseRealtimeService supabaseRealtimeService)
    {
        _supabaseRealtimeService = supabaseRealtimeService;
    }

    public Task CreateAsync(int userId, string message)
    {
        return _supabaseRealtimeService.PublishNotificationAsync(userId, message);
    }
}
