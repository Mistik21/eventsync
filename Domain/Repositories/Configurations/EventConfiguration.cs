using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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

        builder.Property(e => e.Debts)
            .HasConversion(
                debts => JsonSerializer.Serialize(debts, JsonSerializerOptions.Default),
                json => JsonSerializer.Deserialize<Dictionary<Guid, int>>(json, JsonSerializerOptions.Default)
                    ?? new Dictionary<Guid, int>(),
                new ValueComparer<IReadOnlyDictionary<Guid, int>>(
                    (left, right) => ReferenceEquals(left, right) ||
                        (left != null && right != null && left.Count == right.Count &&
                            left.All(debt => right.ContainsKey(debt.Key) && right[debt.Key] == debt.Value)),
                    debts => debts.OrderBy(debt => debt.Key)
                        .Aggregate(0, (hash, debt) => HashCode.Combine(hash, debt.Key, debt.Value)),
                    debts => new Dictionary<Guid, int>(debts)))
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();

        builder.OwnsOne(e => e.EventLocation, location =>
        {
            location.Property(value => value.Address).IsRequired();
            location.Property(value => value.Coordinates).IsRequired();
        });
        builder.Navigation(e => e.EventLocation).IsRequired();
        
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.Organizer)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasMany(e => e.Participants)
            .WithMany()
            .UsingEntity(j => j.ToTable("EventParticipants"));
        
        builder.HasMany(e => e.Cars)
            .WithOne()
            .HasForeignKey("EventId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Activities)
            .WithOne()
            .HasForeignKey("EventId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Items)
            .WithOne()
            .HasForeignKey("EventId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Expenses)
            .WithOne()
            .HasForeignKey("EventId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
