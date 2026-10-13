using Domain.Entities;
using Domain.Entities.Auth;
using Domain.Entities.Clinic;
using Domain.Entities.Doctor;
using Domain.Entities.JWT;
using Domain.Entities.Patient;
using Domain.ValueObjects;
using Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

/// <summary>
/// Główny DbContext aplikacji
/// Definiuje wszystkie DbSets (tabele) w bazie danych
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {

    }
    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<EmailConfirmationToken> EmailConfirmationTokens => Set<EmailConfirmationToken>();
    public DbSet<EmailTwoFactorChallenge> EmailTwoFactorChallenges => Set<EmailTwoFactorChallenge>();
    public DbSet<PasswordResetRequest> PasswordResetRequests => Set<PasswordResetRequest>();

    public DbSet<Clinic> Clinics => Set<Clinic>();
    public DbSet<ClinicMembership> ClinicMemberships => Set<ClinicMembership>();
    public DbSet<ClinicRoom> ClinicRooms => Set<ClinicRoom>();
    public DbSet<DoctorProfile> DoctorProfiles => Set<DoctorProfile>();
    public DbSet<DoctorSpecialization> DoctorSpecializations => Set<DoctorSpecialization>();
    public DbSet<PatientProfile> PatientProfiles => Set<PatientProfile>();
    public DbSet<Specialization> Specializations => Set<Specialization>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<ProjectTask> ProjectTasks => Set<ProjectTask>();

    /// <summary>
    /// Konfiguracja modeli i relacji między encjami
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
