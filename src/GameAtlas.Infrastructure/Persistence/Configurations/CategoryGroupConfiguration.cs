using GameAtlas.Application.CategoryGroups;
using GameAtlas.Domain.CategoryGroups;
using GameAtlas.Domain.Games;
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
		builder.Property(group => group.GameId).IsRequired();
		builder.HasOne<Game>().WithMany().HasForeignKey(group => group.GameId).OnDelete(DeleteBehavior.Cascade);
		builder.Property(group => group.Name).HasMaxLength(CategoryGroupLimits.NameMaxLength).IsRequired();
		builder.Property(group => group.NormalizedName).HasMaxLength(CategoryGroupLimits.NameMaxLength).IsRequired();
		builder.HasIndex(group => new { group.GameId, group.NormalizedName }).IsUnique();
		builder.HasIndex(group => new { group.GameId, group.Order });
		builder.Property(group => group.CreatedAt).HasConversion<UtcTimestampConverter>();
		builder.Property(group => group.UpdatedAt).HasConversion<UtcTimestampConverter>();
	}
}
