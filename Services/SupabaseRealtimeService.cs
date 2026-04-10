using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace EventManagementPortal.Services;

public class SupabaseRealtimeService : ISupabaseRealtimeService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public SupabaseRealtimeService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public Task PublishSeatUpdateAsync(int competitionId, int availableSeats)
    {
        var payload = new
        {
            competitionId,
            availableSeats
        };

        return PublishAsync($"competition:{competitionId}", "seat_update", payload);
    }

    public Task PublishNotificationAsync(int userId, string message)
    {
        var payload = new
        {
            userId,
            message,
            createdAt = DateTime.UtcNow
        };

        return PublishAsync("notifications", "new_notification", payload);
    }

    private async Task PublishAsync(string topic, string eventName, object payload)
    {
        var url = _configuration["Supabase:Url"];
        var serviceRoleKey = _configuration["Supabase:ServiceRoleKey"];

        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(serviceRoleKey))
        {
            return;
        }

        var endpoint = $"{url.TrimEnd('/')}/realtime/v1/api/broadcast";
        var client = _httpClientFactory.CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", serviceRoleKey);
        client.DefaultRequestHeaders.Add("apikey", serviceRoleKey);

        var body = new
        {
            messages = new[]
            {
                new
                {
                    topic,
                    @event = eventName,
                    payload,
                    @private = false
                }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        try
        {
            await client.PostAsync(endpoint, content);
        }
        catch
        {
            // No-op: realtime delivery failure should not break primary business flow.
        }
    }
}
