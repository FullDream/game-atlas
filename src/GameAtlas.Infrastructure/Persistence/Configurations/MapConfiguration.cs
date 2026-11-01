using GameAtlas.Domain.Games;
using GameAtlas.Domain.Maps;
using GameAtlas.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameAtlas.Infrastructure.Persistence.Configurations;

public sealed class MapConfiguration : IEntityTypeConfiguration<Map>
{
	public void Configure(EntityTypeBuilder<Map> builder)
	{
		builder.ToTable("Maps");
		builder.HasKey(map => map.Id);
		builder.Property(map => map.Id).ValueGeneratedNever();
		builder.HasIndex(map => map.GameId);
		builder.Property(map => map.Name).HasMaxLength(Map.NameMaxLength).IsRequired();
		builder.Property(map => map.NormalizedName).HasMaxLength(Map.NameMaxLength).IsRequired();
		builder.HasIndex(map => new { map.GameId, map.NormalizedName }).IsUnique();
		builder.HasOne<Game>().WithMany().HasForeignKey(map => map.GameId).OnDelete(DeleteBehavior.Cascade);
		builder.Property(map => map.CreatedAt).HasConversion<UtcTimestampConverter>();
		builder.Property(map => map.UpdatedAt).HasConversion<UtcTimestampConverter>();
		builder.Ignore(map => map.Geometry);

		builder.HasMany(map => map.Styles).WithOne().HasForeignKey(style => style.MapId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.Navigation(map => map.Styles).HasField("styles").UsePropertyAccessMode(PropertyAccessMode.Field);
	}
}
