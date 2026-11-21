using Domain.Entities.Patient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public sealed class PatientProfileConfiguration : IEntityTypeConfiguration<PatientProfile>
    {
        public void Configure(EntityTypeBuilder<PatientProfile> builder)
        {
            builder.ToTable("PatientProfiles");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.UserId)
                .IsUnique();

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.PhoneNumber)
                .HasMaxLength(20);

            builder.Property(x => x.DateOfBirth)
                .IsRequired();

            builder.Property(x => x.IsActive)
                .IsRequired();

            builder.HasOne(x => x.User)
                .WithOne()
                .HasForeignKey<PatientProfile>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.OwnsOne(patient => patient.Address, address =>
            {
                address.Property(x => x.Street)
                    .IsRequired()
                    .HasMaxLength(150);

                address.Property(x => x.BuildingNumber)
                    .IsRequired()
                    .HasMaxLength(20);

                address.Property(x => x.ApartmentNumber)
                    .HasMaxLength(20);

                address.Property(x => x.City)
                    .IsRequired()
                    .HasMaxLength(100);

                address.Property(x => x.PostalCode)
                    .IsRequired()
                    .HasMaxLength(20);

                address.Property(x => x.Country)
                    .IsRequired()
                    .HasMaxLength(100);
            });
        }
    }
}
