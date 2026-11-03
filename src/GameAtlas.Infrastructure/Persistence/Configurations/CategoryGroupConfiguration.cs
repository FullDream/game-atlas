using GameAtlas.Application.Categories.Groups;
using GameAtlas.Domain.Categories;
using GameAtlas.Domain.Maps;
using GameAtlas.Infrastructure.Persistence.Converters;
using GameAtlas.Domain.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameAtlas.Infrastructure.Persistence.Configurations;

public sealed class CategoryGroupConfiguration : IEntityTypeConfiguration<CategoryGroup>
{
	public void Configure(EntityTypeBuilder<CategoryGroup> builder)
	{
		builder.ToTable("CategoryGroups");
		builder.HasKey(group => group.Id);
		builder.HasOne<ImageAsset>().WithMany().HasForeignKey(group => group.IconAssetId).OnDelete(DeleteBehavior.Restrict);
		builder.Property(group => group.Id).ValueGeneratedNever();
		builder.Property(group => group.MapId).IsRequired();
		builder.HasOne<Map>().WithMany().HasForeignKey(group => group.MapId).OnDelete(DeleteBehavior.Cascade);
		builder.Property(group => group.Name).HasMaxLength(CategoryGroupLimits.NameMaxLength).IsRequired();
		builder.Property(group => group.NormalizedName).HasMaxLength(CategoryGroupLimits.NameMaxLength).IsRequired();
		builder.Property(group => group.Color).HasMaxLength(7).HasDefaultValue("#F59E0B").IsRequired();
		builder.HasIndex(group => new { group.MapId, group.NormalizedName }).IsUnique();
		builder.HasIndex(group => new { group.MapId, group.Order });
		builder.Property(group => group.CreatedAt).HasConversion<UtcTimestampConverter>();
		builder.Property(group => group.UpdatedAt).HasConversion<UtcTimestampConverter>();
	}
}
