using Domain.Entities.Clinic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public sealed class ClinicDbConfiguration : IEntityTypeConfiguration<Clinic>
    {
        public void Configure(EntityTypeBuilder<Clinic> builder)
        {
            builder.ToTable("Clinics");

            builder.HasKey(clinic => clinic.Id);
            
            builder.Property(clinic => clinic.Name)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(clinic => clinic.PhoneNumber)
                .HasMaxLength(256);
            
            builder.Property(clinic => clinic.PhoneNumber2)
                .HasMaxLength(256);
            
            builder.Property(clinic => clinic.PhoneNumber3)
                .HasMaxLength(256);
            
            builder.Property(clinic => clinic.Email)
                .HasMaxLength(256);
            
            builder.Property(clinic => clinic.Website)
                .HasMaxLength(256);
            
            builder.Property(clinic => clinic.OpenTime)
                .IsRequired();
            
            builder.Property(clinic => clinic.CloseTime)
                .IsRequired();

            builder.OwnsOne(clinic => clinic.Address, address =>
            {
                address.Property(x => x.Street)
                    .IsRequired()
                    .HasMaxLength(150);

                address.Property(x => x.BuildingNumber)
                    .IsRequired()
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
