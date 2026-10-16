using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayContext : DbContext
{
    public RaceDayContext(DbContextOptions<RaceDayContext> options) : base(options)
    {
    }

    public DbSet<Club> Clubs => Set<Club>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Club>(e =>
        {
            e.ToTable("Club");
            e.HasKey(x => x.ClubID);
            e.Property(x => x.ClubName).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.ClubName).IsUnique();
            e.Property(x => x.Province).HasMaxLength(50).IsRequired();
            e.Property(x => x.DateEstablished).HasColumnType("date");
        });

        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("User", t => t.HasCheckConstraint("CK_User_Role", "[Role] IN ('Organiser','Participant')"));
            e.HasKey(x => x.UserID);
            e.Property(x => x.FullName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(150).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.PasswordHash).HasMaxLength(256).IsRequired();
            e.Property(x => x.Role).HasMaxLength(20).IsRequired();
            e.Property(x => x.PhoneNumber).HasMaxLength(20);
            e.Property(x => x.DateRegistered).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            e.HasOne(x => x.Club)
                .WithMany(c => c.Users)
                .HasForeignKey(x => x.ClubID)
                .HasConstraintName("FK_User_Club")
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Event>(e =>
        {
            e.ToTable("Event", t => t.HasCheckConstraint("CK_Event_Type", "[EventType] IN ('Running','Walking','Cycling')"));
            e.HasKey(x => x.EventID);
            e.Property(x => x.EventName).HasMaxLength(150).IsRequired();
            e.Property(x => x.EventDate).HasColumnType("date");
            e.Property(x => x.Location).HasMaxLength(150).IsRequired();
            e.Property(x => x.Distance).HasPrecision(6, 2);
            e.Property(x => x.EventType).HasMaxLength(30).IsRequired();
            e.Property(x => x.DateCreated).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            e.HasOne(x => x.Organiser)
                .WithMany()
                .HasForeignKey(x => x.OrganiserID)
                .HasConstraintName("FK_Event_Organiser")
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("Category");
            e.HasKey(x => x.CategoryID);
            e.Property(x => x.CategoryName).HasMaxLength(50).IsRequired();
            e.Property(x => x.EntryFee).HasPrecision(8, 2).HasDefaultValue(0m);
            e.HasIndex(x => new { x.EventID, x.CategoryName }).IsUnique().HasDatabaseName("UQ_Category_Event_Name");
            e.HasOne(x => x.Event)
                .WithMany(ev => ev.Categories)
                .HasForeignKey(x => x.EventID)
                .HasConstraintName("FK_Category_Event")
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Enrolment>(e =>
        {
            e.ToTable("Enrolment");
            e.HasKey(x => x.EnrolmentID);
            e.Property(x => x.EnrolmentDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            e.HasIndex(x => new { x.ParticipantID, x.EventID }).IsUnique().HasDatabaseName("UQ_Enrolment_Participant_Event");
            e.HasOne(x => x.Participant)
                .WithMany()
                .HasForeignKey(x => x.ParticipantID)
                .HasConstraintName("FK_Enrolment_User")
                .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Event)
                .WithMany(ev => ev.Enrolments)
                .HasForeignKey(x => x.EventID)
                .HasConstraintName("FK_Enrolment_Event")
                .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Category)
                .WithMany(c => c.Enrolments)
                .HasForeignKey(x => x.CategoryID)
                .HasConstraintName("FK_Enrolment_Category")
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Result>(e =>
        {
            e.ToTable("Result");
            e.HasKey(x => x.ResultID);
            e.Property(x => x.CapturedDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            e.HasOne(x => x.Enrolment)
                .WithOne(en => en.Result)
                .HasForeignKey<Result>(x => x.EnrolmentID)
                .HasConstraintName("FK_Result_Enrolment")
                .OnDelete(DeleteBehavior.NoAction);
        });
    }
}
