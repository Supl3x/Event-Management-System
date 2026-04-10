using System.Security.Claims;
using EventManagementPortal.Data;
using EventManagementPortal.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EventManagementPortal.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AccountController> _logger;

    public AccountController(ApplicationDbContext db, ILogger<AccountController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == model.Email);
        if (user == null
            || string.IsNullOrEmpty(user.PasswordHash)
            || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        var roles = await ResolveRolesAsync(user.UserID);
        await SignInAsync(user, roles);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register(string? returnUrl = null)
    {
        return View(new RegisterViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (model.Role == AppRoles.Student)
        {
            if (string.IsNullOrWhiteSpace(model.RollNumber))
            {
                ModelState.AddModelError(nameof(model.RollNumber), "Roll number is required for students.");
            }

            if (string.IsNullOrWhiteSpace(model.Department))
            {
                ModelState.AddModelError(nameof(model.Department), "Department is required for students.");
            }
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await _db.Users.AnyAsync(u => u.Email == model.Email))
        {
            ModelState.AddModelError(nameof(model.Email), "This email is already registered.");
            return View(model);
        }

        var user = new User
        {
            Name = model.Name,
            Email = model.Email,
            Phone = model.Phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password)
        };

        _db.Users.Add(user);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            ModelState.AddModelError(
                nameof(model.Email),
                "This email is already registered. If signup timed out, try logging in instead.");
            _db.Entry(user).State = EntityState.Detached;
            return View(model);
        }

        switch (model.Role)
        {
            case AppRoles.Admin:
                _db.Admins.Add(new Admin { UserID = user.UserID });
                break;
            case AppRoles.Organizer:
                _db.Organizers.Add(new OrganizerProfile { UserID = user.UserID });
                break;
            case AppRoles.Volunteer:
                _db.Volunteers.Add(new Volunteer { UserID = user.UserID });
                break;
            default:
                _db.Students.Add(new Student
                {
                    UserID = user.UserID,
                    RollNumber = model.RollNumber!.Trim(),
                    Department = model.Department!.Trim()
                });
                break;
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            ModelState.AddModelError(string.Empty, "That roll number may already be in use, or your account was partially created. Try logging in or contact support.");
            _logger.LogWarning(ex, "Register failed on role/profile insert for email {Email}.", model.Email);
            return View(model);
        }

        _logger.LogInformation("User registered with role {Role}.", model.Role);
        var roles = await ResolveRolesAsync(user.UserID);
        await SignInAsync(user, roles);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    /// <summary>All roles this user has in the database (multiple claims so e.g. Admin + Organizer can still open Verify).</summary>
    private async Task<List<string>> ResolveRolesAsync(int userId)
    {
        var roles = new List<string>();
        if (await _db.Admins.AnyAsync(a => a.UserID == userId))
        {
            roles.Add(AppRoles.Admin);
        }

        if (await _db.Organizers.AnyAsync(o => o.UserID == userId))
        {
            roles.Add(AppRoles.Organizer);
        }

        if (await _db.Volunteers.AnyAsync(v => v.UserID == userId))
        {
            roles.Add(AppRoles.Volunteer);
        }

        if (await _db.Students.AnyAsync(s => s.UserID == userId))
        {
            roles.Add(AppRoles.Student);
        }

        if (roles.Count == 0)
        {
            roles.Add(AppRoles.Student);
        }

        return roles;
    }

    private Task SignInAsync(User user, IReadOnlyList<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserID.ToString()),
            new(ClaimTypes.Name, user.Email),
            new(ClaimTypes.GivenName, user.Name)
        };

        foreach (var role in roles.Distinct())
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        return HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = false
            });
    }
}
