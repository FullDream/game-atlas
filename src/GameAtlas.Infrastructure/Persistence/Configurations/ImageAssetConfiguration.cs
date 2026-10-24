using GameAtlas.Domain.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameAtlas.Infrastructure.Persistence.Configurations;

public sealed class ImageAssetConfiguration : IEntityTypeConfiguration<ImageAsset>
{
	public void Configure(EntityTypeBuilder<ImageAsset> builder)
	{
		builder.Ignore(image => image.IsRaster);
		builder.Property(image => image.Alt).IsRequired();
	}
}
