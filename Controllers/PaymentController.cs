using EventManagementPortal.Data;
using EventManagementPortal.Infrastructure;
using EventManagementPortal.Models;
using EventManagementPortal.Services;
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
    private readonly INotificationService _notificationService;

    public PaymentController(
        ApplicationDbContext context,
        IWebHostEnvironment environment,
        INotificationService notificationService)
    {
        _context = context;
        _environment = environment;
        _notificationService = notificationService;
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

        if (!string.Equals(reg.Status, RegistrationStatuses.Confirmed, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(reg.Status, RegistrationStatuses.Approved, StringComparison.OrdinalIgnoreCase))
        {
            TempData["ErrorMessage"] = "Payment upload is available only for confirmed registrations.";
            return RedirectToAction("MyRegistrations", "Registration");
        }

        var existingPayment = await _context.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.RegistrationID == registrationId);
        if (existingPayment != null && existingPayment.Status != PaymentStatuses.Rejected)
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

        if (!string.Equals(registration.Status, RegistrationStatuses.Confirmed, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(registration.Status, RegistrationStatuses.Approved, StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(string.Empty, "Payment upload is available only for confirmed registrations.");
            return View(model);
        }

        var existingPayment = await _context.Payments
            .FirstOrDefaultAsync(p => p.RegistrationID == model.RegistrationID);
        if (existingPayment != null && existingPayment.Status != PaymentStatuses.Rejected)
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

        if (existingPayment != null && existingPayment.Status == PaymentStatuses.Rejected)
        {
            existingPayment.Screenshot = relativePath;
            existingPayment.Status = PaymentStatuses.Pending;
            existingPayment.SubmittedAt = DateTime.UtcNow;
            existingPayment.VerifiedBy = null;
            existingPayment.VerifiedAt = null;
        }
        else
        {
            var payment = new Payment
            {
                RegistrationID = model.RegistrationID,
                Screenshot = relativePath,
                Status = PaymentStatuses.Pending,
                SubmittedAt = DateTime.UtcNow
            };
            _context.Payments.Add(payment);
        }
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
        var verifierId = User.GetUserId();
        if (verifierId is null)
        {
            return Challenge();
        }

        var isOrganizer = await _context.Organizers.AnyAsync(o => o.UserID == verifierId.Value);
        var isVolunteer = await _context.Volunteers.AnyAsync(v => v.UserID == verifierId.Value);
        if (!isOrganizer && !isVolunteer)
        {
            return Forbid();
        }

        var query = _context.Payments
            .Include(p => p.Registration)
            .ThenInclude(r => r!.User)
            .Include(p => p.Registration)
            .ThenInclude(r => r!.Competition)
            .OrderByDescending(p => p.SubmittedAt)
            .AsQueryable();

        if (isVolunteer && !isOrganizer)
        {
            query = query.Where(p =>
                _context.EventStaffAssignments.Any(es =>
                    es.EventID == p.Registration!.Competition!.EventID
                    && es.UserID == verifierId.Value
                    && !string.Equals(es.Role, "PendingApproval", StringComparison.OrdinalIgnoreCase))
                || _context.CompetitionStaffAssignments.Any(cs =>
                    cs.CompetitionID == p.Registration!.CompetitionID
                    && cs.UserID == verifierId.Value
                    && !string.Equals(cs.Role, "PendingApproval", StringComparison.OrdinalIgnoreCase)));
        }

        var payments = await query.ToListAsync();

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
            .ThenInclude(r => r!.Competition)
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

        var isOrganizerVerifier = await _context.Organizers.AnyAsync(o => o.UserID == verifierId.Value);
        var isVolunteerVerifier = await _context.Volunteers.AnyAsync(v => v.UserID == verifierId.Value);
        if (isVolunteerVerifier && !isOrganizerVerifier)
        {
            var comp = payment.Registration?.Competition;
            if (comp == null)
            {
                TempData["ErrorMessage"] = "This payment is not linked to a valid competition.";
                return RedirectToAction(nameof(Verify));
            }

            var hasApproval = await _context.EventStaffAssignments.AnyAsync(es =>
                                  es.EventID == comp.EventID
                                  && es.UserID == verifierId.Value
                                  && !string.Equals(es.Role, "PendingApproval", StringComparison.OrdinalIgnoreCase))
                              || await _context.CompetitionStaffAssignments.AnyAsync(cs =>
                                  cs.CompetitionID == comp.CompetitionID
                                  && cs.UserID == verifierId.Value
                                  && !string.Equals(cs.Role, "PendingApproval", StringComparison.OrdinalIgnoreCase));
            if (!hasApproval)
            {
                TempData["ErrorMessage"] = "You need organizer approval for this event before verifying payments.";
                return RedirectToAction(nameof(Verify));
            }
        }

        payment.Status = PaymentStatuses.Approved;
        payment.VerifiedBy = verifierId.Value;
        payment.VerifiedAt = DateTime.UtcNow;

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

        if (payment.Registration != null)
        {
            await _notificationService.CreateAsync(
                payment.Registration.UserID,
                $"Your payment for registration #{payment.RegistrationID} has been approved and your ticket is ready.");
        }

        TempData["SuccessMessage"] = "Payment approved and ticket issued.";
        return RedirectToAction(nameof(Verify));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.OrganizerOrVolunteer)]
    public async Task<IActionResult> RejectPayment(int paymentId)
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
            .ThenInclude(r => r!.Competition)
            .FirstOrDefaultAsync(p => p.PaymentID == paymentId);
        if (payment == null)
        {
            return NotFound();
        }

        if (payment.Status != PaymentStatuses.Pending)
        {
            TempData["ErrorMessage"] = "Only pending payments can be rejected.";
            return RedirectToAction(nameof(Verify));
        }

        var isOrganizerVerifier = await _context.Organizers.AnyAsync(o => o.UserID == verifierId.Value);
        var isVolunteerVerifier = await _context.Volunteers.AnyAsync(v => v.UserID == verifierId.Value);
        if (isVolunteerVerifier && !isOrganizerVerifier)
        {
            var comp = payment.Registration?.Competition;
            if (comp == null)
            {
                TempData["ErrorMessage"] = "This payment is not linked to a valid competition.";
                return RedirectToAction(nameof(Verify));
            }

            var hasApproval = await _context.EventStaffAssignments.AnyAsync(es =>
                                  es.EventID == comp.EventID
                                  && es.UserID == verifierId.Value
                                  && !string.Equals(es.Role, "PendingApproval", StringComparison.OrdinalIgnoreCase))
                              || await _context.CompetitionStaffAssignments.AnyAsync(cs =>
                                  cs.CompetitionID == comp.CompetitionID
                                  && cs.UserID == verifierId.Value
                                  && !string.Equals(cs.Role, "PendingApproval", StringComparison.OrdinalIgnoreCase));
            if (!hasApproval)
            {
                TempData["ErrorMessage"] = "You need organizer approval for this event before rejecting payments.";
                return RedirectToAction(nameof(Verify));
            }
        }

        payment.Status = PaymentStatuses.Rejected;
        payment.VerifiedBy = verifierId.Value;
        payment.VerifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        if (payment.Registration != null)
        {
            await _notificationService.CreateAsync(
                payment.Registration.UserID,
                $"Your payment for registration #{payment.RegistrationID} was rejected. Please re-upload proof.");
        }

        TempData["SuccessMessage"] = "Payment rejected.";
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
