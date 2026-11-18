using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using eventsynс.Domain.Entities;

namespace eventsynс.Domain.Repositories.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events");

        builder.HasKey(u => u.Id);

        builder.Property(e => e.Title)
            .IsRequired();

        builder.Property(e => e.Description)
            .IsRequired();

        builder.Property(e => e.MainImageUrl);

        builder.Property(e => e.EventStartTime)
            .IsRequired();

        builder.Property(e => e.Budget)
            .IsRequired();

        builder.Property(e => e.CollectedBudget)
            .IsRequired();

        builder.Property(e => e.CollectionStatus)
            .IsRequired();
        
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.Organizer)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasMany(e => e.Participants)
            .WithMany()
            .UsingEntity(j => j.ToTable("EventParticipants"));
        
        builder.HasMany(e => e.Cars)
            .WithOne()
            .HasForeignKey("EventId");

        builder.HasMany(e => e.Activities)
            .WithOne()
            .HasForeignKey("EventId");

        builder.HasMany(e => e.Items)
            .WithOne()
            .HasForeignKey("EventId");

        builder.HasMany(e => e.Expenses)
            .WithOne()
            .HasForeignKey("EventId");
    }
}