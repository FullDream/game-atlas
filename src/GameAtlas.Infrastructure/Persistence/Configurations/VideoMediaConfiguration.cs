using GameAtlas.Domain.Markers;
using GameAtlas.Domain.Markers.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameAtlas.Infrastructure.Persistence.Configurations;

public sealed class VideoMediaConfiguration : IEntityTypeConfiguration<VideoMedia>
{
	public void Configure(EntityTypeBuilder<VideoMedia> builder)
	{
		builder.Property(video => video.Url).HasColumnName("Url")
			.HasMaxLength(MarkerLimits.ReferenceMaxLength).IsRequired();

		builder.Property(video => video.Preview).HasMaxLength(MarkerLimits.ReferenceMaxLength);
	}
}
