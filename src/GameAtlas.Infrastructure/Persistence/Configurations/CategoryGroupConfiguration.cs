using GameAtlas.Application.CategoryGroups;
using GameAtlas.Domain.CategoryGroups;
using GameAtlas.Domain.Maps;
using GameAtlas.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameAtlas.Infrastructure.Persistence.Configurations;

public sealed class CategoryGroupConfiguration : IEntityTypeConfiguration<CategoryGroup>
{
	public void Configure(EntityTypeBuilder<CategoryGroup> builder)
	{
		builder.ToTable("CategoryGroups");
		builder.HasKey(group => group.Id);
		builder.Property(group => group.Id).ValueGeneratedNever();
		builder.Property(group => group.MapId).IsRequired();
		builder.HasOne<Map>().WithMany().HasForeignKey(group => group.MapId).OnDelete(DeleteBehavior.Cascade);
		builder.Property(group => group.Name).HasMaxLength(CategoryGroupLimits.NameMaxLength).IsRequired();
		builder.Property(group => group.NormalizedName).HasMaxLength(CategoryGroupLimits.NameMaxLength).IsRequired();
		builder.HasIndex(group => new { group.MapId, group.NormalizedName }).IsUnique();
		builder.HasIndex(group => new { group.MapId, group.Order });
		builder.Property(group => group.CreatedAt).HasConversion<UtcTimestampConverter>();
		builder.Property(group => group.UpdatedAt).HasConversion<UtcTimestampConverter>();
	}
}
