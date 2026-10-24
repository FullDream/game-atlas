using GameAtlas.Domain.Result;

namespace GameAtlas.Domain.Maps;

public sealed class Map
{
	public const int NameMaxLength = 200;
	private readonly List<MapStyle> styles = [];

	private Map(Guid id, int width, int height)
	{
		Id = id;
		Width = width;
		Height = height;
		Geometry = new MapGeometry(width, height);
	}

	public Guid Id { get; }
	public Guid GameId { get; private set; }
	public string Name { get; private set; } = string.Empty;
	public string NormalizedName { get; private set; } = string.Empty;
	public int Width { get; private set; }
	public int Height { get; private set; }
	public long LastCreationSequence { get; private set; }
	public DateTimeOffset CreatedAt { get; private set; }
	public DateTimeOffset UpdatedAt { get; private set; }
	public IReadOnlyCollection<MapStyle> Styles => styles.AsReadOnly();
	public MapGeometry Geometry { get; }

	public Guid? DefaultStyleId => styles
		.Where(style => style.Status == MapProcessingStatus.Ready)
		.OrderBy(style => style.CreationSequence)
		.Select(style => (Guid?)style.Id)
		.FirstOrDefault();

	public static string NormalizeName(string name) => name.Trim().ToUpperInvariant();

	public static Result<Map> Create(Guid gameId, string? name, int width, int height)
	{
		name = name?.Trim();

		if (string.IsNullOrEmpty(name) || name.Length > NameMaxLength)
			return MapErrors.InvalidMapName;

		var geometry = MapGeometry.Create(width, height);

		if (geometry.IsFailure)
			return geometry.Errors;

		var now = DateTimeOffset.UtcNow;
		var map = new Map(Guid.CreateVersion7(), width, height)
		{
			GameId = gameId,
			Name = name,
			NormalizedName = NormalizeName(name),
			CreatedAt = now,
			UpdatedAt = now
		};

		return map;
	}

	public Result<MapStyle> AddStyle(MapStyleInput input)
	{
		if (input.Width != Width || input.Height != Height)
			return MapErrors.DimensionsMismatch;

		var created = MapStyle.Create(Id, LastCreationSequence + 1, input.Name, input.BackgroundColor,
			input.Format, input.Width, input.Height, input.ByteLength);

		if (created.IsFailure)
			return created.Errors;

		var appended = AppendPreparedStyle(created.Value);

		return appended.IsFailure ? appended.Errors : created.Value;
	}

	public Result.Result AppendPreparedStyle(MapStyle style)
	{
		if (style.MapId != Id)
			return MapErrors.StyleOwnershipMismatch;

		if (styles.Any(existing => existing.Id == style.Id))
			return MapErrors.DuplicateStyle;

		if (style.Width != Width || style.Height != Height)
			return MapErrors.DimensionsMismatch;

		if (styles.Any(existing => existing.NormalizedName == style.NormalizedName))
			return MapErrors.NameConflict;

		style.AssignCreationSequence(++LastCreationSequence);
		styles.Add(style);
		UpdatedAt = DateTimeOffset.UtcNow;

		return Result.Result.Success();
	}

	public Result<bool> UpdateStyle(Guid styleId, Optional<string?> name = default,
		Optional<string?> backgroundColor = default)
	{
		var style = styles.SingleOrDefault(item => item.Id == styleId);

		if (style is null)
			return MapErrors.MissingStyle;

		var nextName = name.GetValueOrDefault(style.Name);
		var conflictingName = nextName is not null && styles.Any(other =>
			other.Id != styleId && other.NormalizedName == MapStyle.NormalizeName(nextName));

		if (conflictingName)
			return MapErrors.NameConflict;

		var result = style.UpdateMetadata(name, backgroundColor);

		if (result.IsSuccess && result.Value)
			UpdatedAt = DateTimeOffset.UtcNow;

		return result;
	}

	public Result.Result DeleteStyle(Guid styleId)
	{
		var style = styles.SingleOrDefault(item => item.Id == styleId);

		if (style is null)
			return MapErrors.MissingStyle;

		styles.Remove(style);
		UpdatedAt = DateTimeOffset.UtcNow;

		return Result.Result.Success();
	}
}
