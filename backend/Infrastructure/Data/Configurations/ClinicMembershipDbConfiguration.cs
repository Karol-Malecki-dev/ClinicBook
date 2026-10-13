using Domain.Entities.Clinic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class ClinicMembershipDbConfiguration : IEntityTypeConfiguration<ClinicMembership>

    {
        public void Configure(EntityTypeBuilder<ClinicMembership> builder)
        {
            builder.ToTable("ClinicMemberships");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new
            {
                x.UserId,
                x.ClinicId
            })
            .IsUnique();

            builder.Property(x => x.Role)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.IsActive)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Clinic)
                .WithMany(clinic => clinic.Memberships)
                .HasForeignKey(x => x.ClinicId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
