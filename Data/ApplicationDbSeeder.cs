using Microsoft.EntityFrameworkCore;

namespace EventManagementPortal.Data;

/// <summary>
/// Optional startup seeding. Does not use AspNetUsers or AspNetRoles; roles are represented by
/// rows in <c>Admin</c>, <c>Organizer</c>, and <c>Student</c> tables.
/// </summary>
public static class ApplicationDbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        const string adminEmail = "saadali@cloud.neduet.edu.pk";
        const string adminPassword = "hello123";

        var adminUser = await db.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
        if (adminUser == null)
        {
            adminUser = new Models.User
            {
                Name = "Saad Ali",
                Email = adminEmail,
                Phone = "0000000000",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword)
            };
            db.Users.Add(adminUser);
            await db.SaveChangesAsync();
        }
        else
        {
            // Keep this bootstrap account usable with the agreed credentials.
            adminUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword);
            await db.SaveChangesAsync();
        }

        var extraAdmins = await db.Admins
            .Where(a => a.UserID != adminUser.UserID)
            .ToListAsync();
        if (extraAdmins.Count > 0)
        {
            db.Admins.RemoveRange(extraAdmins);
        }

        var hasAdminRow = await db.Admins.AnyAsync(a => a.UserID == adminUser.UserID);
        if (!hasAdminRow)
        {
            db.Admins.Add(new Models.Admin { UserID = adminUser.UserID });
        }

        await db.SaveChangesAsync();
    }
}
