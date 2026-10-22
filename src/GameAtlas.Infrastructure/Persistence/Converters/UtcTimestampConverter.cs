using System.Globalization;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GameAtlas.Infrastructure.Persistence.Converters;

internal sealed class UtcTimestampConverter() : ValueConverter<DateTimeOffset, string>(
	value => value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.FFFFFFFzzz", CultureInfo.InvariantCulture),
	value => DateTimeOffset.Parse(value, CultureInfo.InvariantCulture));
