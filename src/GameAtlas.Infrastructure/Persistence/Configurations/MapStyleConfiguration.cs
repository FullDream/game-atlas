using GameAtlas.Domain.Maps;
using GameAtlas.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameAtlas.Infrastructure.Persistence.Configurations;

public sealed class MapStyleConfiguration : IEntityTypeConfiguration<MapStyle>
{
	public void Configure(EntityTypeBuilder<MapStyle> builder)
	{
		builder.ToTable("MapStyles");
		builder.HasKey(style => style.Id);
		builder.Property(style => style.Id).ValueGeneratedNever();
		builder.Property(style => style.Name).HasMaxLength(MapStyle.NameMaxLength).IsRequired();
		builder.Property(style => style.NormalizedName).HasMaxLength(MapStyle.NameMaxLength).IsRequired();
		builder.Property(style => style.BackgroundColor).HasMaxLength(7).IsRequired();
		builder.HasIndex(style => new { style.MapId, style.NormalizedName }).IsUnique();
		builder.HasIndex(style => new { style.MapId, style.CreationSequence }).IsUnique();
		builder.Property(style => style.CreatedAt).HasConversion<UtcTimestampConverter>();
		builder.Property(style => style.UpdatedAt).HasConversion<UtcTimestampConverter>();
		builder.Property(style => style.Format).HasMaxLength(4).IsRequired();
		builder.Property(style => style.Status).HasConversion<string>().IsRequired();
		builder.Property(style => style.AttemptId).IsConcurrencyToken();
		builder.Property(style => style.Status).IsConcurrencyToken();
		builder.Property(style => style.ErrorCode).HasMaxLength(100);
		builder.Property(style => style.ErrorMessage).HasMaxLength(300);
		builder.HasIndex(style => new { style.Status, style.CreatedAt });
	}
}
