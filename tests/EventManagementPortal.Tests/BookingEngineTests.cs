using EventManagementPortal.Models;
using EventManagementPortal.Services;
using Xunit;

namespace EventManagementPortal.Tests;

public class BookingEngineTests
{
    [Fact]
    public void DecideNewRegistration_UsesConfirmedWhenSeatsAvailable()
    {
        var decision = BookingEngine.DecideNewRegistration(availableSeats: 3, currentMaxWaitlistPriority: 7);

        Assert.Equal(RegistrationStatuses.Confirmed, decision.RegistrationStatus);
        Assert.Null(decision.WaitlistPriority);
        Assert.Equal(2, decision.UpdatedAvailableSeats);
    }

    [Fact]
    public void DecideNewRegistration_UsesWaitlistWhenNoSeats()
    {
        var decision = BookingEngine.DecideNewRegistration(availableSeats: 0, currentMaxWaitlistPriority: 2);

        Assert.Equal(RegistrationStatuses.Waitlist, decision.RegistrationStatus);
        Assert.Equal(3, decision.WaitlistPriority);
        Assert.Equal(0, decision.UpdatedAvailableSeats);
    }

    [Fact]
    public void DecideNewRegistration_AssignsFirstWaitlistPriorityAsOne()
    {
        var decision = BookingEngine.DecideNewRegistration(availableSeats: 0, currentMaxWaitlistPriority: 0);

        Assert.Equal(1, decision.WaitlistPriority);
    }
}
