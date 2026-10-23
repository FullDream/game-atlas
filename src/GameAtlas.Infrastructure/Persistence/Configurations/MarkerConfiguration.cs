using GameAtlas.Domain.Categories;
using GameAtlas.Domain.Markers;
using GameAtlas.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameAtlas.Infrastructure.Persistence.Configurations;

public sealed class MarkerConfiguration : IEntityTypeConfiguration<Marker>
{
	public void Configure(EntityTypeBuilder<Marker> builder)
	{
		builder.ToTable("Markers");
		builder.HasKey(marker => marker.Id);
		builder.Property(marker => marker.Id).ValueGeneratedNever();
		builder.Property(marker => marker.Name).HasMaxLength(MarkerLimits.NameMaxLength).IsRequired();
		builder.Property(marker => marker.Description).HasMaxLength(MarkerLimits.DescriptionMaxLength);
		builder.Property(marker => marker.CreatedAt).HasConversion<UtcTimestampConverter>();
		builder.Property(marker => marker.UpdatedAt).HasConversion<UtcTimestampConverter>();
		builder.HasOne<Category>().WithMany().HasForeignKey(marker => marker.CategoryId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.ComplexProperty(marker => marker.Position, position =>
		{
			position.Property(value => value.X).HasColumnName("PositionX");
			position.Property(value => value.Y).HasColumnName("PositionY");
		});

		builder.HasMany(marker => marker.Media).WithOne().HasForeignKey(item => item.MarkerId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.Navigation(marker => marker.Media).HasField("media").UsePropertyAccessMode(PropertyAccessMode.Field);
	}
}
