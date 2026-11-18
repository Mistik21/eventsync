using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using eventsynс.Domain.Entities;

namespace eventsynс.Domain.Repositories.Configurations;

public class CostConfiguration : IEntityTypeConfiguration<Cost>
{
    public void Configure(EntityTypeBuilder<Cost> builder)
    {
        builder.ToTable("Costs");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Title)
            .IsRequired();

        builder.Property(u => u.Amount)
            .IsRequired();
    }
}