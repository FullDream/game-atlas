using GameAtlas.Domain.Markers;
using GameAtlas.Domain.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameAtlas.Infrastructure.Persistence.Configurations;

public sealed class MarkerAssetConfiguration : IEntityTypeConfiguration<MarkerAsset>
{
	public void Configure(EntityTypeBuilder<MarkerAsset> builder)
	{
		builder.ToTable("MarkerAssets");
		builder.HasKey(item => new { item.MarkerId, item.AssetId });
		builder.Property(item => item.MarkerId).ValueGeneratedNever();
		builder.Property(item => item.AssetId).ValueGeneratedNever();
		builder.HasIndex(item => new { item.MarkerId, item.Order });
		builder.HasOne<MediaAsset>().WithMany().HasForeignKey(item => item.AssetId).OnDelete(DeleteBehavior.Restrict);
	}
}
