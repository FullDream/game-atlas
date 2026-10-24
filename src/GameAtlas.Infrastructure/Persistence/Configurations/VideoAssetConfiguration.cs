using GameAtlas.Domain.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameAtlas.Infrastructure.Persistence.Configurations;

public sealed class VideoAssetConfiguration : IEntityTypeConfiguration<VideoAsset>
{
	public void Configure(EntityTypeBuilder<VideoAsset> builder) =>
		builder.HasOne(video => video.Preview).WithMany().HasForeignKey(video => video.PreviewAssetId)
			.OnDelete(DeleteBehavior.Restrict);
}
