using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using EventManagementPortal.Data;
using EventManagementPortal.Models;
using EventManagementPortal.Services;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
// Add services to the container.
var connectionString =
    builder.Configuration.GetValue<string>("ConnectionStrings:DefaultConnection")
    ?? builder.Configuration.GetValue<string>("ConnectionStrings__DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

Console.WriteLine($"✅ Loaded DB connection string (length: {connectionString.Length}).");
// Supabase pooler (6543) can be slow from distant regions; keep Command Timeout high enough.
// Do not use EnableRetryOnFailure here: after a read timeout the INSERT may already be committed,
// and retrying SaveChanges() causes duplicate key on users_email (23505).
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions.CommandTimeout(120))
);
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("OrganizerOnly", policy => policy.RequireRole(AppRoles.Organizer));
    options.AddPolicy("StudentOnly", policy => policy.RequireRole(AppRoles.Student));
    options.AddPolicy("OrganizerOrAdmin", policy => policy.RequireRole(AppRoles.Organizer, AppRoles.Admin));
});
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ISupabaseRealtimeService, SupabaseRealtimeService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        await ApplyVersionedMigrationsAsync(db, app.Environment.ContentRootPath);
        await ApplicationDbSeeder.SeedAsync(db);
    }
    catch (Exception ex)
    {
        LogStartupException(ex);
        throw;
    }
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        await db.Database.ExecuteSqlRawAsync("SELECT 1");
        Console.WriteLine("✅ Database connection warmed up.");
    }
    catch { /* ignore startup ping failures */ }
}
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// "http" launch profile has no HTTPS URL; UseHttpsRedirection logs a warning and is unnecessary in Development.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static async Task ApplyVersionedMigrationsAsync(ApplicationDbContext db, string contentRootPath)
{
    var migrationsDir = Path.Combine(contentRootPath, "Database", "Migrations");
    if (!Directory.Exists(migrationsDir))
    {
        return;
    }

    var migrationFiles = Directory.GetFiles(migrationsDir, "*.sql")
        .OrderBy(Path.GetFileName)
        .ToList();
    if (migrationFiles.Count == 0)
    {
        return;
    }

    try
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS public.schema_migrations (
              id SERIAL PRIMARY KEY,
              filename VARCHAR(255) NOT NULL UNIQUE,
              appliedat TIMESTAMP NOT NULL DEFAULT NOW()
            );
            """);

        foreach (var file in migrationFiles)
        {
            var filename = Path.GetFileName(file);
            var alreadyApplied = await db.Database
                .SqlQueryRaw<int>("SELECT 1 FROM public.schema_migrations WHERE filename = {0}", filename)
                .AnyAsync();
            if (alreadyApplied)
            {
                continue;
            }

            var sql = await File.ReadAllTextAsync(file);
            if (!string.IsNullOrWhiteSpace(sql))
            {
                await db.Database.ExecuteSqlRawAsync(sql);
            }
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO public.schema_migrations(filename, appliedat) VALUES ({0}, NOW())", filename);
        }
    }
    catch (Exception ex)
    {
        // Best-effort startup migration runner for environments without EF migrations.
        LogStartupException(ex);
    }
}

static void LogStartupException(Exception ex)
{
    // Intentionally avoid Exception.ToString() because your container logs show it can fail.
    Console.Error.WriteLine("❌ Startup exception:");
    Console.Error.WriteLine($"Type: {ex.GetType().FullName}");
    Console.Error.WriteLine($"Message: {ex.Message}");

    var inner = ex.InnerException;
    if (inner is not null)
    {
        Console.Error.WriteLine("--- Inner exception ---");
        Console.Error.WriteLine($"Type: {inner.GetType().FullName}");
        Console.Error.WriteLine($"Message: {inner.Message}");
    }
}
