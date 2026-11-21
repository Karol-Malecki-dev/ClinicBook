using Domain.Entities.Clinic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments");

        builder.HasKey(appointment => appointment.Id);

        builder.HasIndex(appointment => new
        {
            appointment.DoctorProfileId,
            appointment.StartsAtUtc
        })
            .IsUnique()
            .HasDatabaseName("IX_Appointments_DoctorProfileId_StartsAtUtc_Booked")
            .HasFilter("\"Status\" = 'Booked'");

        builder.HasIndex(appointment => new
        {
            appointment.PatientUserId,
            appointment.StartsAtUtc
        });

        builder.Property(appointment => appointment.StartsAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(appointment => appointment.EndsAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(appointment => appointment.CreatedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(appointment => appointment.UpdatedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(appointment => appointment.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.HasOne(appointment => appointment.Clinic)
            .WithMany(clinic => clinic.Appointments)
            .HasForeignKey(appointment => appointment.ClinicId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(appointment => appointment.DoctorProfile)
            .WithMany(doctor => doctor.Appointments)
            .HasForeignKey(appointment => appointment.DoctorProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(appointment => appointment.Patient)
            .WithMany()
            .HasForeignKey(appointment => appointment.PatientUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
