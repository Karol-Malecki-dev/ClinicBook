using Domain.Entities.Doctor;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public sealed class DoctorSpecializationConfiguration
    : IEntityTypeConfiguration<DoctorSpecialization>
{
    public void Configure(EntityTypeBuilder<DoctorSpecialization> builder)
    {
        builder.ToTable("DoctorSpecializations");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new
        {
            x.DoctorProfileId,
            x.SpecializationId
        })
        .IsUnique();

        builder.Property(x => x.ExperienceDescription)
            .HasMaxLength(500);

        builder.HasOne(x => x.DoctorProfile)
            .WithMany(doctor => doctor.DoctorSpecializations)
            .HasForeignKey(x => x.DoctorProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Specialization)
            .WithMany(specialization => specialization.DoctorSpecializations)
            .HasForeignKey(x => x.SpecializationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
