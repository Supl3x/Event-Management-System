using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using EventManagementPortal.Models;
using EventManagementPortal.Data;
using Microsoft.EntityFrameworkCore;

namespace EventManagementPortal.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var competitions = await _context.Competitions
            .AsNoTracking()
            .Include(c => c.Event)
            .OrderBy(c => c.StartDate)
            .ToListAsync();

        return View(competitions);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
