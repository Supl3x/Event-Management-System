using EventManagementPortal.Data;
using EventManagementPortal.Models;

namespace EventManagementPortal.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly ISupabaseRealtimeService _supabaseRealtimeService;

    public NotificationService(ApplicationDbContext db, ISupabaseRealtimeService supabaseRealtimeService)
    {
        _db = db;
        _supabaseRealtimeService = supabaseRealtimeService;
    }

    public async Task CreateAsync(int userId, string message)
    {
        _db.Notifications.Add(new Notification
        {
            UserID = userId,
            Message = message,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        });
        await _db.SaveChangesAsync();
        await _supabaseRealtimeService.PublishNotificationAsync(userId, message);
    }
}
