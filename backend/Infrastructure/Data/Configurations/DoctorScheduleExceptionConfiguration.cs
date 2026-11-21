using Domain.Entities.Clinic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public sealed class DoctorScheduleExceptionConfiguration
    : IEntityTypeConfiguration<DoctorScheduleException>
{
    public void Configure(EntityTypeBuilder<DoctorScheduleException> builder)
    {
        builder.ToTable("DoctorScheduleExceptions");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.Date).IsRequired();
        builder.Property(item => item.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(item => item.StartTime).IsRequired();
        builder.Property(item => item.EndTime).IsRequired();
        builder.Property(item => item.SlotDurationMinutes).IsRequired();
        builder.Property(item => item.Reason).HasMaxLength(300);
        builder.Property(item => item.CreatedAtUtc).IsRequired();

        builder.HasIndex(item => new
        {
            item.ClinicId,
            item.DoctorProfileId,
            item.Date,
            item.Type,
            item.StartTime
        }).HasDatabaseName("IX_DoctorScheduleException_Doctor_Date_Type_Start");

        builder.HasOne(item => item.Clinic)
            .WithMany(clinic => clinic.ScheduleExceptions)
            .HasForeignKey(item => item.ClinicId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(item => item.DoctorProfile)
            .WithMany(doctor => doctor.ScheduleExceptions)
            .HasForeignKey(item => item.DoctorProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
