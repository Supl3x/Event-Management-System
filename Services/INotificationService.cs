namespace EventManagementPortal.Services;

public interface INotificationService
{
    Task CreateAsync(int userId, string message);
}
