using GameAtlas.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GameAtlas.Infrastructure.Persistence.Converters;

public sealed class SlugConverter() : ValueConverter<Slug, string>(slug => slug.Value, value => Read(value))
{
	private static Slug Read(string value)
	{
		var result = Slug.Create(value);

		if (result.IsFailure || result.Value.Value != value)
			throw new InvalidOperationException("The stored slug is invalid.");

		return result.Value;
	}
}
