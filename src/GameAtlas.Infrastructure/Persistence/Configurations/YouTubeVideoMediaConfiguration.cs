using GameAtlas.Domain.Markers.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameAtlas.Infrastructure.Persistence.Configurations;

public sealed class YouTubeVideoMediaConfiguration : IEntityTypeConfiguration<YouTubeVideoMedia>
{
	public void Configure(EntityTypeBuilder<YouTubeVideoMedia> builder) =>
		builder.Property(video => video.VideoId).HasMaxLength(11).IsRequired();
}
