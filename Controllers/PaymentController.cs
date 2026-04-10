using EventManagementPortal.Data;
using EventManagementPortal.Infrastructure;
using EventManagementPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EventManagementPortal.Controllers;

[Authorize]
public class PaymentController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public PaymentController(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [HttpGet]
    public async Task<IActionResult> Create(int registrationId)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var reg = await _context.Registrations
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RegistrationID == registrationId && r.UserID == userId.Value);

        if (reg == null)
        {
            return NotFound();
        }

        if (await _context.Payments.AnyAsync(p => p.RegistrationID == registrationId))
        {
            TempData["ErrorMessage"] = "A payment has already been submitted for this registration.";
            return RedirectToAction("MyRegistrations", "Registration");
        }

        return View(new PaymentCreateViewModel { RegistrationID = registrationId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PaymentCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = User.GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var registration = await _context.Registrations
            .FirstOrDefaultAsync(r => r.RegistrationID == model.RegistrationID && r.UserID == userId.Value);

        if (registration == null)
        {
            return NotFound();
        }

        if (await _context.Payments.AnyAsync(p => p.RegistrationID == model.RegistrationID))
        {
            ModelState.AddModelError(string.Empty, "A payment has already been submitted for this registration.");
            return View(model);
        }

        var extension = Path.GetExtension(model.Screenshot.FileName).ToLowerInvariant();
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        if (!allowed.Contains(extension))
        {
            ModelState.AddModelError(nameof(model.Screenshot), "Only jpg, jpeg, png, and webp files are allowed.");
            return View(model);
        }

        var uploadsRoot = Path.Combine(_environment.WebRootPath, "uploads", "payments");
        Directory.CreateDirectory(uploadsRoot);

        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsRoot, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await model.Screenshot.CopyToAsync(stream);
        }

        var relativePath = $"/uploads/payments/{fileName}";

        var payment = new Payment
        {
            RegistrationID = model.RegistrationID,
            Screenshot = relativePath,
            Status = PaymentStatuses.Pending,
            SubmittedAt = DateTime.UtcNow
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Payment proof uploaded. Pending organizer or volunteer verification.";
        return RedirectToAction(nameof(MyPayments));
    }

    [HttpGet]
    public async Task<IActionResult> MyPayments()
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var payments = await _context.Payments
            .AsNoTracking()
            .Include(p => p.Registration)
            .ThenInclude(r => r!.Competition)
            .Where(p => p.Registration!.UserID == userId.Value)
            .OrderByDescending(p => p.SubmittedAt)
            .ToListAsync();

        return View(payments);
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.OrganizerOrVolunteer)]
    public async Task<IActionResult> Verify()
    {
        var payments = await _context.Payments
            .Include(p => p.Registration)
            .ThenInclude(r => r!.User)
            .OrderByDescending(p => p.SubmittedAt)
            .ToListAsync();

        return View(payments);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.OrganizerOrVolunteer)]
    public async Task<IActionResult> VerifyPayment(int paymentId)
    {
        var verifierId = User.GetUserId();
        if (verifierId is null)
        {
            return Challenge();
        }

        var allowedInDb = await _context.Organizers.AnyAsync(o => o.UserID == verifierId.Value)
            || await _context.Volunteers.AnyAsync(v => v.UserID == verifierId.Value);
        if (!allowedInDb)
        {
            TempData["ErrorMessage"] =
                "Your account is not recorded as an organizer or volunteer, so the database cannot record verification.";
            return RedirectToAction(nameof(Verify));
        }

        var payment = await _context.Payments
            .Include(p => p.Registration)
            .FirstOrDefaultAsync(p => p.PaymentID == paymentId);

        if (payment == null)
        {
            return NotFound();
        }

        if (payment.Status != PaymentStatuses.Pending)
        {
            TempData["SuccessMessage"] = "Payment is not pending.";
            return RedirectToAction(nameof(Verify));
        }

        payment.Status = PaymentStatuses.Approved;
        payment.VerifiedBy = verifierId.Value;
        payment.VerifiedAt = DateTime.UtcNow;

        payment.Registration.Status = RegistrationStatuses.Approved;

        // Save approval first so DB triggers (if any) can create the ticket without conflicting with our INSERT
        // in the same batch (duplicate ticket_registrationid_key).
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == "P0001")
        {
            TempData["ErrorMessage"] = pg.MessageText;
            return RedirectToAction(nameof(Verify));
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg
            && pg.SqlState == PostgresErrorCodes.UniqueViolation
            && IsTicketRegistrationUniqueConflict(pg))
        {
            _context.ChangeTracker.Clear();
            var alreadyDone = await _context.Payments.AsNoTracking()
                .AnyAsync(p => p.PaymentID == paymentId && p.Status == PaymentStatuses.Approved);
            TempData[alreadyDone ? "SuccessMessage" : "ErrorMessage"] = alreadyDone
                ? "Payment approved; a ticket already exists for this registration."
                : "Could not complete verification because a ticket row already exists. Refresh and check the list.";
            return RedirectToAction(nameof(Verify));
        }

        var regId = payment.RegistrationID;
        if (!await _context.Tickets.AsNoTracking().AnyAsync(t => t.RegistrationID == regId))
        {
            var code = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            _context.Tickets.Add(new Ticket
            {
                RegistrationID = regId,
                QrCode = Guid.NewGuid().ToString(),
                UniqueCode = code,
                GeneratedAt = DateTime.UtcNow
            });
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg
                && pg.SqlState == PostgresErrorCodes.UniqueViolation
                && IsTicketRegistrationUniqueConflict(pg))
            {
                // Concurrent verify or trigger created the ticket between check and insert.
            }
        }

        TempData["SuccessMessage"] = "Payment approved. Registration approved and ticket issued.";
        return RedirectToAction(nameof(Verify));
    }

    private static bool IsTicketRegistrationUniqueConflict(PostgresException pg)
    {
        if (pg.ConstraintName?.Contains("ticket_registrationid", StringComparison.OrdinalIgnoreCase) == true)
        {
            return true;
        }

        return pg.MessageText.Contains("ticket_registrationid", StringComparison.OrdinalIgnoreCase);
    }
}
