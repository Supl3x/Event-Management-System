using Microsoft.EntityFrameworkCore;
using EventManagementPortal.Models;

namespace EventManagementPortal.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Admin> Admins { get; set; }
    public DbSet<OrganizerProfile> Organizers { get; set; }
    public DbSet<Volunteer> Volunteers { get; set; }
    public DbSet<Student> Students { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<Competition> Competitions { get; set; }
    public DbSet<Registration> Registrations { get; set; }
    public DbSet<Ticket> Tickets { get; set; }
    public DbSet<Team> Teams { get; set; }
    public DbSet<TeamMember> TeamMembers { get; set; }
    public DbSet<Payment> Payments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("public");

        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.UserID);
            e.Property(x => x.UserID).HasColumnName("userid");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(256);
            e.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(50);
            e.Property(x => x.PasswordHash).HasColumnName("passwordhash").HasMaxLength(500);
            e.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Admin>(e =>
        {
            e.ToTable("admin");
            e.HasKey(x => x.UserID);
            e.Property(x => x.UserID).HasColumnName("userid");
            e.HasOne(a => a.User).WithOne(u => u.AdminProfile).HasForeignKey<Admin>(a => a.UserID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrganizerProfile>(e =>
        {
            e.ToTable("organizer");
            e.HasKey(x => x.UserID);
            e.Property(x => x.UserID).HasColumnName("userid");
            e.HasOne(o => o.User).WithOne(u => u.OrganizerProfile).HasForeignKey<OrganizerProfile>(o => o.UserID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Volunteer>(e =>
        {
            e.ToTable("volunteer");
            e.HasKey(x => x.UserID);
            e.Property(x => x.UserID).HasColumnName("userid");
            e.HasOne(v => v.User).WithOne(u => u.VolunteerProfile).HasForeignKey<Volunteer>(v => v.UserID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Student>(e =>
        {
            e.ToTable("student");
            e.HasKey(x => x.UserID);
            e.Property(x => x.UserID).HasColumnName("userid");
            e.Property(x => x.RollNumber).HasColumnName("rollnumber").HasMaxLength(100);
            e.Property(x => x.Department).HasColumnName("department").HasMaxLength(100);
            e.HasIndex(x => x.RollNumber).IsUnique();
            e.HasOne(s => s.User).WithOne(u => u.Student).HasForeignKey<Student>(s => s.UserID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Event>(e =>
        {
            e.ToTable("event");
            e.HasKey(x => x.EventID);
            e.Property(x => x.EventID).HasColumnName("eventid");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(500);
            e.Property(x => x.Department).HasColumnName("department").HasMaxLength(200);
            e.Property(x => x.Location).HasColumnName("location").HasMaxLength(500);
            e.Property(x => x.StartDate).HasColumnName("startdate").HasColumnType("date");
            e.Property(x => x.EndDate).HasColumnName("enddate").HasColumnType("date");
            e.Property(x => x.CreatedBy).HasColumnName("createdby");
            e.HasOne(ev => ev.Creator).WithMany(o => o.EventsCreated).HasForeignKey(ev => ev.CreatedBy)
                .HasPrincipalKey(o => o.UserID).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Competition>(e =>
        {
            e.ToTable("competition");
            e.HasKey(x => x.CompetitionID);
            e.Property(x => x.CompetitionID).HasColumnName("competitionid");
            e.Property(x => x.EventID).HasColumnName("eventid");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(500);
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.Location).HasColumnName("location").HasMaxLength(500);
            e.Property(x => x.StartDate).HasColumnName("startdate").HasColumnType("date");
            e.Property(x => x.EndDate).HasColumnName("enddate").HasColumnType("date");
            e.Property(x => x.MaxTeamSize).HasColumnName("maxteamsize");
            e.Property(x => x.EntryFee).HasColumnName("entryfee").HasColumnType("numeric(12,2)");
            e.HasOne(c => c.Event).WithMany(ev => ev.Competitions).HasForeignKey(c => c.EventID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Registration>(e =>
        {
            e.ToTable("registration");
            e.HasKey(x => x.RegistrationID);
            e.Property(x => x.RegistrationID).HasColumnName("registrationid");
            e.Property(x => x.CompetitionID).HasColumnName("competitionid");
            e.Property(x => x.UserID).HasColumnName("userid");
            e.Property(x => x.TeamID).HasColumnName("teamid");
            e.Property(x => x.Type).HasColumnName("type").HasMaxLength(50);
            e.Property(x => x.Status).HasColumnName("status").HasMaxLength(50);
            e.Property(x => x.RegisteredAt).HasColumnName("registeredat");
            e.HasOne(r => r.User).WithMany(u => u.Registrations).HasForeignKey(r => r.UserID);
            e.HasOne(r => r.Competition).WithMany(c => c.Registrations).HasForeignKey(r => r.CompetitionID);
            e.HasOne(r => r.Team).WithMany(t => t.Registrations).HasForeignKey(r => r.TeamID)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Payment>(e =>
        {
            e.ToTable("payment");
            e.HasKey(x => x.PaymentID);
            e.Property(x => x.PaymentID).HasColumnName("paymentid");
            e.Property(x => x.RegistrationID).HasColumnName("registrationid");
            e.Property(x => x.Screenshot).HasColumnName("screenshot").HasMaxLength(1000);
            e.Property(x => x.Status).HasColumnName("status").HasMaxLength(50);
            e.Property(x => x.VerifiedBy).HasColumnName("verifiedby");
            e.Property(x => x.SubmittedAt).HasColumnName("submittedat");
            e.Property(x => x.VerifiedAt).HasColumnName("verifiedat");
            e.HasIndex(x => x.RegistrationID).IsUnique();
            e.HasOne(p => p.Registration).WithOne(r => r.Payment).HasForeignKey<Payment>(p => p.RegistrationID)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.Verifier).WithMany(u => u.PaymentsVerified).HasForeignKey(p => p.VerifiedBy)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Ticket>(e =>
        {
            e.ToTable("ticket");
            e.HasKey(x => x.TicketID);
            e.Property(x => x.TicketID).HasColumnName("ticketid");
            e.Property(x => x.RegistrationID).HasColumnName("registrationid");
            e.Property(x => x.QrCode).HasColumnName("qrcode");
            e.Property(x => x.UniqueCode).HasColumnName("uniquecode").HasMaxLength(50);
            e.Property(x => x.GeneratedAt).HasColumnName("generatedat");
            e.HasIndex(x => x.RegistrationID).IsUnique();
            e.HasOne(t => t.Registration).WithOne(r => r.Ticket).HasForeignKey<Ticket>(t => t.RegistrationID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Team>(e =>
        {
            e.ToTable("team");
            e.HasKey(x => x.TeamID);
            e.Property(x => x.TeamID).HasColumnName("teamid");
            e.Property(x => x.TeamName).HasColumnName("teamname").HasMaxLength(200);
            e.Property(x => x.LeaderUserID).HasColumnName("leaderuserid");
            e.Property(x => x.CompetitionID).HasColumnName("competitionid");
            e.HasOne(t => t.Leader).WithMany().HasForeignKey(t => t.LeaderUserID)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.Competition).WithMany(c => c.Teams).HasForeignKey(t => t.CompetitionID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TeamMember>(e =>
        {
            e.ToTable("teammember");
            e.HasKey(x => x.MemberID);
            e.Property(x => x.MemberID).HasColumnName("memberid");
            e.Property(x => x.TeamID).HasColumnName("teamid");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            e.Property(x => x.RollNumber).HasColumnName("rollnumber").HasMaxLength(100);
            e.Property(x => x.Department).HasColumnName("department").HasMaxLength(100);
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(256);
            e.HasOne(tm => tm.Team).WithMany(t => t.Members).HasForeignKey(tm => tm.TeamID)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
