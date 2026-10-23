using GameAtlas.Domain.Markers;
using GameAtlas.Domain.Markers.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameAtlas.Infrastructure.Persistence.Configurations;

public sealed class ImageMediaConfiguration : IEntityTypeConfiguration<ImageMedia>
{
	public void Configure(EntityTypeBuilder<ImageMedia> builder)
	{
		builder.Property(image => image.Url).HasColumnName("Url")
			.HasMaxLength(MarkerLimits.ReferenceMaxLength).IsRequired();

		builder.Property(image => image.Width).IsRequired();
		builder.Property(image => image.Height).IsRequired();
		builder.Property(image => image.Alt).IsRequired();
	}
}
