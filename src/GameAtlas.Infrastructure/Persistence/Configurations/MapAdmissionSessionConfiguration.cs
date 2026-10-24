using GameAtlas.Infrastructure.Maps.Persistence;
using GameAtlas.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameAtlas.Infrastructure.Persistence.Configurations;

internal sealed class MapAdmissionSessionConfiguration : IEntityTypeConfiguration<MapAdmissionSession>
{
	public void Configure(EntityTypeBuilder<MapAdmissionSession> builder)
	{
		builder.ToTable("MapAdmissionSessions");
		builder.HasKey(session => session.Id);
		builder.Property(session => session.Id).ValueGeneratedNever();
		builder.Property(session => session.ExpiresAt).HasConversion<UtcTimestampConverter>();
		builder.HasIndex(session => session.ExpiresAt);
		builder.HasMany(session => session.Sources).WithOne().HasForeignKey(source => source.SessionId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
