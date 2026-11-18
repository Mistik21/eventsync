using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using eventsynс.Domain.Entities;

namespace eventsynс.Domain.Repositories.Configurations;

public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("Items");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Title)
            .IsRequired();

        builder.Property(u => u.Description);
        
        builder.Property(u => u.Count)
            .IsRequired();
        
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(i => i.ResponsibleID)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(u => u.UrlShop);
    }
}