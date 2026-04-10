namespace EventManagementPortal.Data;

/// <summary>
/// Optional startup seeding. Does not use AspNetUsers or AspNetRoles; roles are represented by
/// rows in <c>Admin</c>, <c>Organizer</c>, and <c>Student</c> tables.
/// </summary>
public static class ApplicationDbSeeder
{
    public static Task SeedAsync(ApplicationDbContext _)
    {
        return Task.CompletedTask;
    }
}
