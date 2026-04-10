using EventManagementPortal.Models;
using EventManagementPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagementPortal.Controllers;

[Authorize]
public class TicketController : Controller
{
    private readonly ITicketService _ticketService;

    public TicketController(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Organizer)]
    public async Task<IActionResult> Cancel(int ticketId, bool hardDelete = false)
    {
        var result = await _ticketService.CancelTicketAsync(ticketId, hardDelete);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction("Index", "Competition");
        }

        TempData["SuccessMessage"] = result.PromotedRegistrationId.HasValue
            ? $"Ticket cancelled. Registration #{result.PromotedRegistrationId.Value} promoted from waitlist."
            : "Ticket cancelled. No waitlist promotion was needed.";

        return RedirectToAction("Index", "Competition");
    }
}
