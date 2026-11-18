using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Activity = eventsynс.Domain.Entities.Activity;
using eventsynс.Domain.Entities;

namespace eventsynс.Domain.Repositories.Configurations;

public class ActivityConfiguration : IEntityTypeConfiguration<Activity>
{
    public void Configure(EntityTypeBuilder<Activity> builder)
    {
        builder.ToTable("Activities");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Title)
            .IsRequired();

        builder.Property(u => u.Description);

        builder.Property(u => u.АctivityStartTime)
            .IsRequired();

        builder.Property(u => u.АctivityEndTime)
            .IsRequired();

        builder.Property(u => u.MaxParticipants)
            .IsRequired();
        
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(a => a.Creator)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasMany(a => a.Participants)
            .WithMany()
            .UsingEntity(j => j.ToTable("ActivityParticipants"));
    }
}