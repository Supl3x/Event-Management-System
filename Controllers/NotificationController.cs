using EventManagementPortal.Data;
using EventManagementPortal.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventManagementPortal.Controllers;

[Authorize]
public class NotificationController : Controller
{
    private readonly ApplicationDbContext _context;

    public NotificationController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var notifications = await _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserID == userId.Value)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return View(notifications);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var item = await _context.Notifications
            .FirstOrDefaultAsync(n => n.NotificationID == id && n.UserID == userId.Value);
        if (item != null && !item.IsRead)
        {
            item.IsRead = true;
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }
}
