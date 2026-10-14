using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }
    public DbSet<User> Users => Set<User>();
    public DbSet<EventType> EventTypes => Set<EventType>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---------- Users ----------
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("Users", t => t.HasCheckConstraint("CK_Users_Role", "[Role] IN ('Organiser', 'Participant')"));
            e.HasKey(u => u.UserId);
            e.Property(u => u.FullName).HasMaxLength(150).IsRequired();
            e.Property(u => u.Email).HasMaxLength(255).IsRequired();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.PasswordHash).HasMaxLength(500).IsRequired();
            e.Property(u => u.Role).HasMaxLength(20).IsRequired();
            e.Property(u => u.PhoneNumber).HasMaxLength(20);
            e.Property(u => u.ProfilePictureUrl).HasMaxLength(500);
        });

        // ---------- EventTypes (lookup, seeded) ----------
        modelBuilder.Entity<EventType>(e =>
        {
            e.ToTable("EventTypes");
            e.HasKey(t => t.EventTypeId);
            e.Property(t => t.TypeName).HasMaxLength(20).IsRequired();
            e.HasIndex(t => t.TypeName).IsUnique();
            e.HasData(
                new EventType { EventTypeId = 1, TypeName = "Run" },
                new EventType { EventTypeId = 2, TypeName = "Walk" },
                new EventType { EventTypeId = 3, TypeName = "Cycle" });
        });

        // ---------- Events ----------
        modelBuilder.Entity<Event>(e =>
        {
            e.ToTable("Events");
            e.HasKey(ev => ev.EventId);
            e.Property(ev => ev.Name).HasMaxLength(150).IsRequired();
            e.Property(ev => ev.Description).HasMaxLength(1000);
            e.Property(ev => ev.Location).HasMaxLength(200).IsRequired();
            e.Property(ev => ev.DistanceKm).HasPrecision(6, 2);
            e.Property(ev => ev.ImageUrl).HasMaxLength(500);

            e.HasOne(ev => ev.Organiser)
                .WithMany(u => u.OrganisedEvents)
                .HasForeignKey(ev => ev.OrganiserId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(ev => ev.EventType)
                .WithMany(t => t.Events)
                .HasForeignKey(ev => ev.EventTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- Categories ----------
        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("Categories");
            e.HasKey(c => c.CategoryId);
            e.Property(c => c.CategoryName).HasMaxLength(100).IsRequired();

            e.HasOne(c => c.Event)
                .WithMany(ev => ev.Categories)
                .HasForeignKey(c => c.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- Enrolments ----------
        modelBuilder.Entity<Enrolment>(e =>
        {
            e.ToTable("Enrolments", t => t.HasCheckConstraint(
                "CK_Enrolments_Status", "[Status] IN ('Pending', 'Confirmed', 'Cancelled')"));
            e.HasKey(en => en.EnrolmentId);
            e.Property(en => en.Status).HasMaxLength(20).IsRequired().HasDefaultValue(EnrolmentStatuses.Pending);

            // A Participant can only enrol once per Event
            e.HasIndex(en => new { en.ParticipantId, en.EventId }).IsUnique();

            // Restrict on Participant and Category avoids SQL Server "multiple cascade paths" errors
            e.HasOne(en => en.Participant)
                .WithMany(u => u.Enrolments)
                .HasForeignKey(en => en.ParticipantId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(en => en.Event)
                .WithMany(ev => ev.Enrolments)
                .HasForeignKey(en => en.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(en => en.Category)
                .WithMany(c => c.Enrolments)
                .HasForeignKey(en => en.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- Results (one-to-one with Enrolment) ----------
        modelBuilder.Entity<Result>(e =>
        {
            e.ToTable("Results");
            e.HasKey(r => r.ResultId);
            e.Property(r => r.FinishTime).HasMaxLength(20).IsRequired();
            e.HasIndex(r => r.EnrolmentId).IsUnique();

            e.HasOne(r => r.Enrolment)
                .WithOne(en => en.Result)
                .HasForeignKey<Result>(r => r.EnrolmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}