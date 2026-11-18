using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using eventsynс.Domain.Entities;

namespace eventsynс.Domain.Repositories.Configurations;

public class CarConfiguration : IEntityTypeConfiguration<Car>
{
    public void Configure(EntityTypeBuilder<Car> builder)
    {
        builder.ToTable("Cars");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.MaxNumberSeats)
            .IsRequired();

        builder.Property(u => u.Description);
        
        builder.OwnsOne(c => c.LocationEnd, location =>
        {
            location.Property(u => u.Address)
                .IsRequired();

            location.Property(u => u.Coordinates)
                .IsRequired();
        });
        
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(u => u.Driver)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasMany(u => u.Passengers)
            .WithMany()
            .UsingEntity(u => u.ToTable("CarPassengers"));
        
        builder.Ignore(u => u.OccupiedSeats);
        builder.Ignore(u => u.AvailableSeats);
    }
}