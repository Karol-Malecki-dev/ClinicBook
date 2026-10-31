using Domain.Entities.Clinic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public sealed class DoctorWeeklyAvailabilityConfiguration
    : IEntityTypeConfiguration<DoctorWeeklyAvailability>
{
    public void Configure(EntityTypeBuilder<DoctorWeeklyAvailability> builder)
    {
        builder.ToTable("DoctorWeeklyAvailabilities");

        builder.HasKey(availability => availability.Id);

        builder.HasIndex(availability => new
        {
            availability.ClinicId,
            availability.DoctorProfileId,
            availability.DayOfWeek,
            availability.StartTime
        })
            .IsUnique()
            .HasDatabaseName("IX_DoctorAvail_Clinic_Doctor_Day_Start");

        builder.Property(availability => availability.StartTime).IsRequired();
        builder.Property(availability => availability.EndTime).IsRequired();
        builder.Property(availability => availability.SlotDurationMinutes).IsRequired();

        builder.HasOne(availability => availability.Clinic)
            .WithMany(clinic => clinic.DoctorWeeklyAvailabilities)
            .HasForeignKey(availability => availability.ClinicId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(availability => availability.DoctorProfile)
            .WithMany(doctor => doctor.WeeklyAvailabilities)
            .HasForeignKey(availability => availability.DoctorProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
