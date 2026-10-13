using Domain.Entities.Clinic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class ClinicRoomConfiguration : IEntityTypeConfiguration<ClinicRoom>
    {
        public void Configure(EntityTypeBuilder<ClinicRoom> builder)
        {
            builder.ToTable("ClinicRooms");
            
            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new
            {
                x.ClinicId,
                x.Name
            })
            .IsUnique();

            builder.Property(x => x.Name)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Description)
                .HasMaxLength(500)
                .IsRequired(false);

            builder.HasOne(x => x.Clinic)
                .WithMany(clinic => clinic.Rooms)
                .HasForeignKey(x => x.ClinicId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
