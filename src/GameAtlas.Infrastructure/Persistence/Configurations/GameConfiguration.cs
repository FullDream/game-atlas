using GameAtlas.Domain.Games;
using GameAtlas.Application.Games;
using GameAtlas.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameAtlas.Infrastructure.Persistence.Configurations;

public sealed class GameConfiguration : IEntityTypeConfiguration<Game>
{
	public void Configure(EntityTypeBuilder<Game> builder)
	{
		builder.ToTable("Games");
		builder.HasKey(game => game.Id);
		builder.Property(game => game.Id).ValueGeneratedNever();
		builder.Property(game => game.Name).HasMaxLength(GameMetadataLimits.NameMaxLength).IsRequired();
		builder.Property(game => game.Slug).HasMaxLength(GameMetadataLimits.SlugMaxLength).IsRequired();
		builder.HasIndex(game => game.Slug).IsUnique();
		builder.HasOne(game => game.Cover).WithMany().HasForeignKey(game => game.CoverId)
			.OnDelete(DeleteBehavior.Restrict).IsRequired();
		builder.HasOne(game => game.Banner).WithMany().HasForeignKey(game => game.BannerId)
			.OnDelete(DeleteBehavior.Restrict);
		builder.Property(game => game.Status).HasConversion<string>().IsRequired();
		builder.Property(game => game.CreatedAt).HasConversion<UtcTimestampConverter>();
		builder.Property(game => game.UpdatedAt).HasConversion<UtcTimestampConverter>();
	}
}
