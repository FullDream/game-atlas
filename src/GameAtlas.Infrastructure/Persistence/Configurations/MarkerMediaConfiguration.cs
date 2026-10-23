using GameAtlas.Domain.Markers;
using GameAtlas.Domain.Markers.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameAtlas.Infrastructure.Persistence.Configurations;

public sealed class MarkerMediaConfiguration : IEntityTypeConfiguration<MarkerMedia>
{
	public void Configure(EntityTypeBuilder<MarkerMedia> builder)
	{
		builder.ToTable("MarkerMedia");
		builder.HasKey(item => item.Id);
		builder.Property(item => item.Id).ValueGeneratedNever();
		builder.HasIndex(item => new { item.MarkerId, item.Order });
		builder.HasDiscriminator<string>("Type")
			.HasValue<ImageMedia>("image")
			.HasValue<YouTubeVideoMedia>("youtubeVideo")
			.HasValue<VideoMedia>("video");

		builder.Property<string>("Type").HasMaxLength(20);
	}
}
