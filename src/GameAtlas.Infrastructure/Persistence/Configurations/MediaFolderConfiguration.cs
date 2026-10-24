using GameAtlas.Domain.Media;
using GameAtlas.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameAtlas.Infrastructure.Persistence.Configurations;

public sealed class MediaFolderConfiguration : IEntityTypeConfiguration<MediaFolder>
{
	public void Configure(EntityTypeBuilder<MediaFolder> builder)
	{
		builder.ToTable("MediaFolders");
		builder.HasKey(folder => folder.Id);
		builder.Property(folder => folder.Id).ValueGeneratedNever();
		builder.Property(folder => folder.Name).HasMaxLength(MediaFolder.NameMaxLength).IsRequired();
		builder.Property(folder => folder.NormalizedName).HasMaxLength(MediaFolder.NameMaxLength).IsRequired();
		builder.HasIndex(folder => folder.NormalizedName).IsUnique().HasFilter("ParentId IS NULL");
		builder.HasIndex(folder => new { folder.ParentId, folder.NormalizedName }).IsUnique();
		builder.HasIndex(folder => new { folder.ParentId, folder.Name, folder.Id });
		builder.HasOne<MediaFolder>().WithMany().HasForeignKey(folder => folder.ParentId)
			.OnDelete(DeleteBehavior.Restrict);

		builder.Property(folder => folder.CreatedAt).HasConversion<UtcTimestampConverter>();
		builder.Property(folder => folder.UpdatedAt).HasConversion<UtcTimestampConverter>();
	}
}
