using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Services;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class ApplicationDbContext : DbContext
{
    private readonly IdentityNumberProtector _identityNumberProtector;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IdentityNumberProtector identityNumberProtector) : base(options)
    {
        _identityNumberProtector = identityNumberProtector;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<Organiser> Organisers => Set<Organiser>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Discipline> Disciplines => Set<Discipline>();
    public DbSet<Registration> Registrations => Set<Registration>();
    public DbSet<Result> Results => Set<Result>();
    public DbSet<Club> Clubs => Set<Club>();
    public DbSet<ClubMembership> ClubMemberships => Set<ClubMembership>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User");
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(255);
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash").HasMaxLength(255);
            entity.Property(e => e.FirstName).HasColumnName("first_name").HasMaxLength(100);
            entity.Property(e => e.LastName).HasColumnName("last_name").HasMaxLength(100);
            entity.Property(e => e.Role).HasColumnName("role").HasMaxLength(20);
            entity.Property(e => e.ProfilePictureUrl).HasColumnName("profile_picture_url").HasMaxLength(2048);
            entity.Property(e => e.IdNumberOrPassport)
                .HasColumnName("id_number_or_passport")
                .HasMaxLength(512)
                .HasConversion(
                    value => _identityNumberProtector.Protect(value),
                    value => _identityNumberProtector.Unprotect(value));
            entity.Property(e => e.AsaPermanentLicenseNumber).HasColumnName("asa_permanent_license_number").HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<Participant>(entity =>
        {
            entity.ToTable("Participant");
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.HasOne(e => e.User)
                .WithOne(u => u.Participant)
                .HasForeignKey<Participant>(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Organiser>(entity =>
        {
            entity.ToTable("Organiser");
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ProvisionedBy).HasColumnName("provisioned_by").HasMaxLength(255);
            entity.Property(e => e.ProvisionedAt).HasColumnName("provisioned_at");
            entity.HasOne(e => e.User)
                .WithOne(u => u.Organiser)
                .HasForeignKey<Organiser>(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Discipline>(entity =>
        {
            entity.ToTable("Discipline");
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100);
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(255);
        });

        modelBuilder.Entity<Club>(entity =>
        {
            entity.ToTable("Club");
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.RegistrationNumber).IsUnique();
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200);
            entity.Property(e => e.RegistrationNumber).HasColumnName("registration_number").HasMaxLength(100);
            entity.Property(e => e.ContactEmail).HasColumnName("contact_email").HasMaxLength(255);
            entity.Property(e => e.ContactNumber).HasColumnName("contact_number").HasMaxLength(30);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("Event");
            entity.HasIndex(e => e.OrganiserId);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganiserId).HasColumnName("organiser_id");
            entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(200);
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(1000);
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.DistanceKm).HasColumnName("distance_km").HasPrecision(6, 2);
            entity.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(10);
            entity.Property(e => e.VenueName).HasColumnName("venue_name").HasMaxLength(200);
            entity.Property(e => e.City).HasColumnName("city").HasMaxLength(100);
            entity.Property(e => e.Province).HasColumnName("province").HasMaxLength(100);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20);
            entity.Property(e => e.BannerUrl).HasColumnName("banner_url").HasMaxLength(2048);
            entity.HasOne(e => e.Organiser)
                .WithMany(o => o.Events)
                .HasForeignKey(e => e.OrganiserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Category");
            entity.HasIndex(e => e.EventId);
            entity.HasIndex(e => e.DisciplineId);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.EventId).HasColumnName("event_id");
            entity.Property(e => e.DisciplineId).HasColumnName("discipline_id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(150);
            entity.Property(e => e.CategoryType).HasColumnName("category_type").HasMaxLength(10);
            entity.Property(e => e.DistanceKm).HasColumnName("distance_km").HasPrecision(6, 2);
            entity.Property(e => e.MinAge).HasColumnName("min_age");
            entity.Property(e => e.MaxAge).HasColumnName("max_age");
            entity.Property(e => e.EntryFeeZar).HasColumnName("entry_fee_zar").HasPrecision(10, 2);
            entity.Property(e => e.MaxSlots).HasColumnName("max_slots");
            entity.Property(e => e.StartTime).HasColumnName("start_time");
            entity.HasOne(e => e.Event)
                .WithMany(ev => ev.Categories)
                .HasForeignKey(e => e.EventId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Discipline)
                .WithMany()
                .HasForeignKey(e => e.DisciplineId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Registration>(entity =>
        {
            entity.ToTable("Registration");
            entity.HasIndex(e => new { e.ParticipantId, e.CategoryId })
                .IsUnique()
                .HasFilter("[status] = 'Active'");
            entity.HasIndex(e => e.CategoryId);
            entity.HasIndex(e => e.ParticipantId);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ParticipantId).HasColumnName("participant_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.RegistrationDate).HasColumnName("registration_date");
            entity.Property(e => e.PaymentStatus).HasColumnName("payment_status").HasMaxLength(20);
            entity.Property(e => e.AmountPaid).HasColumnName("amount_paid").HasPrecision(10, 2);
            entity.Property(e => e.AssignedBibNumber).HasColumnName("assigned_bib_number").HasMaxLength(20);
            entity.Property(e => e.RequiresTempAsaLicense).HasColumnName("requires_temp_asa_license");
            entity.Property(e => e.MedicalAidName).HasColumnName("medical_aid_name").HasMaxLength(150);
            entity.Property(e => e.MedicalAidNumber).HasColumnName("medical_aid_number").HasMaxLength(50);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20);
            entity.HasOne(e => e.Participant)
                .WithMany(p => p.Registrations)
                .HasForeignKey(e => e.ParticipantId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Category)
                .WithMany(c => c.Registrations)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Result>(entity =>
        {
            entity.ToTable("Result");
            entity.HasIndex(e => e.RegistrationId).IsUnique();
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RegistrationId).HasColumnName("registration_id");
            entity.Property(e => e.GunTime).HasColumnName("gun_time");
            entity.Property(e => e.NetTime).HasColumnName("net_time");
            entity.Property(e => e.OverallPosition).HasColumnName("overall_position");
            entity.Property(e => e.CategoryPosition).HasColumnName("category_position");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20);
            entity.HasOne(e => e.Registration)
                .WithOne(r => r.Result)
                .HasForeignKey<Result>(e => e.RegistrationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ClubMembership>(entity =>
        {
            entity.ToTable("ClubMembership");
            entity.HasIndex(e => new { e.ParticipantId, e.DisciplineId }).IsUnique();
            entity.HasIndex(e => e.ClubId);
            entity.HasIndex(e => e.DisciplineId);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ParticipantId).HasColumnName("participant_id");
            entity.Property(e => e.ClubId).HasColumnName("club_id");
            entity.Property(e => e.DisciplineId).HasColumnName("discipline_id");
            entity.Property(e => e.MembershipNumber).HasColumnName("membership_number").HasMaxLength(100);
            entity.Property(e => e.JoinedAt).HasColumnName("joined_at");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20);
            entity.HasOne(e => e.Participant)
                .WithMany(p => p.Memberships)
                .HasForeignKey(e => e.ParticipantId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Club)
                .WithMany()
                .HasForeignKey(e => e.ClubId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Discipline)
                .WithMany()
                .HasForeignKey(e => e.DisciplineId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
