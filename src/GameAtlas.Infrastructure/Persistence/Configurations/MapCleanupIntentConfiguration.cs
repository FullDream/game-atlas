using GameAtlas.Infrastructure.Maps.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameAtlas.Infrastructure.Persistence.Configurations;

internal sealed class MapCleanupIntentConfiguration : IEntityTypeConfiguration<MapCleanupIntent>
{
	public void Configure(EntityTypeBuilder<MapCleanupIntent> builder)
	{
		builder.ToTable("MapCleanupIntents");
		builder.HasKey(intent => intent.Id);
		builder.Property(intent => intent.Id).ValueGeneratedNever();
		builder.HasIndex(intent => intent.RetryAt);
	}
}
