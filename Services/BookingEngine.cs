using EventManagementPortal.Models;

namespace EventManagementPortal.Services;

public static class BookingEngine
{
    public static BookingDecision DecideNewRegistration(int availableSeats, int currentMaxWaitlistPriority)
    {
        if (availableSeats > 0)
        {
            return new BookingDecision
            {
                RegistrationStatus = RegistrationStatuses.Confirmed,
                WaitlistPriority = null,
                UpdatedAvailableSeats = availableSeats - 1
            };
        }

        return new BookingDecision
        {
            RegistrationStatus = RegistrationStatuses.Waitlist,
            WaitlistPriority = currentMaxWaitlistPriority + 1,
            UpdatedAvailableSeats = availableSeats
        };
    }
}

public sealed class BookingDecision
{
    public string RegistrationStatus { get; set; } = RegistrationStatuses.Waitlist;
    public int? WaitlistPriority { get; set; }
    public int UpdatedAvailableSeats { get; set; }
}
