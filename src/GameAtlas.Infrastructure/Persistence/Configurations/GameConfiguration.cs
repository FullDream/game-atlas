using GameAtlas.Domain.Games;
using GameAtlas.Application.Games;
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
		builder.Property(game => game.Image).HasMaxLength(GameMetadataLimits.ImageMaxLength).IsRequired();
		builder.Property(game => game.Status).HasConversion<string>().IsRequired();
	}
}
