using GameAtlas.Infrastructure.Maps.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameAtlas.Infrastructure.Persistence.Configurations;

internal sealed class MapAdmissionStyleConfiguration : IEntityTypeConfiguration<MapAdmissionStyle>
{
	public void Configure(EntityTypeBuilder<MapAdmissionStyle> builder)
	{
		builder.ToTable("MapAdmissionStyles");
		builder.HasKey(source => source.Id);
		builder.Property(source => source.Id).ValueGeneratedNever();
		builder.HasIndex(style => style.StyleId).IsUnique();
	}
}
