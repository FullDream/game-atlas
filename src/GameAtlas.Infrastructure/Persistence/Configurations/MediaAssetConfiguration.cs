using GameAtlas.Domain.Media;
using GameAtlas.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameAtlas.Infrastructure.Persistence.Configurations;

public sealed class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
	public void Configure(EntityTypeBuilder<MediaAsset> builder)
	{
		builder.ToTable("MediaAssets");
		builder.HasKey(asset => asset.Id);
		builder.Property(asset => asset.Id).ValueGeneratedNever();
		builder.Property(asset => asset.Name).HasMaxLength(MediaFolder.NameMaxLength).IsRequired();
		builder.Property(asset => asset.ContentType).HasMaxLength(64).IsRequired();
		builder.HasIndex(asset => new { asset.FolderId, asset.Name, asset.Id });
		builder.HasOne<MediaFolder>().WithMany().HasForeignKey(asset => asset.FolderId)
			.OnDelete(DeleteBehavior.Restrict);

		builder.Property(asset => asset.CreatedAt).HasConversion<UtcTimestampConverter>();
		builder.Property(asset => asset.UpdatedAt).HasConversion<UtcTimestampConverter>();
		builder.HasDiscriminator<string>("Type").HasValue<ImageAsset>("image").HasValue<VideoAsset>("video");
	}
}
