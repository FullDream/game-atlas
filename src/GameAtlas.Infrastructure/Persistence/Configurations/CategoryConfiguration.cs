using GameAtlas.Application.Categories;
using GameAtlas.Domain.Categories;
using GameAtlas.Domain.CategoryGroups;
using GameAtlas.Domain.Media;
using GameAtlas.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameAtlas.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
	public void Configure(EntityTypeBuilder<Category> builder)
	{
		builder.ToTable("Categories");
		builder.HasKey(category => category.Id);
		builder.Property(category => category.Id).ValueGeneratedNever();
		builder.HasOne<CategoryGroup>().WithMany().HasForeignKey(category => category.CategoryGroupId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(category => category.Name).HasMaxLength(CategoryLimits.NameMaxLength).IsRequired();
		builder.Property(category => category.NormalizedName).HasMaxLength(CategoryLimits.NameMaxLength).IsRequired();
		builder.HasOne<ImageAsset>().WithMany().HasForeignKey(category => category.IconAssetId)
			.OnDelete(DeleteBehavior.Restrict);

		builder.Property(category => category.Color).HasMaxLength(7).IsRequired();
		builder.HasIndex(category => new { category.CategoryGroupId, category.NormalizedName }).IsUnique();
		builder.HasIndex(category => new { category.CategoryGroupId, category.Order });
		builder.Property(category => category.CreatedAt).HasConversion<UtcTimestampConverter>();
		builder.Property(category => category.UpdatedAt).HasConversion<UtcTimestampConverter>();
	}
}
