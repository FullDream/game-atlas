using GameAtlas.Domain.Result;

namespace GameAtlas.Domain.Maps;

public sealed class Map
{
	public const int NameMaxLength = 200;
	private readonly List<MapStyle> styles = [];

	private MapGeometry? geometry;

	private Map(Guid id) => Id = id;

	public Guid Id { get; }
	public Guid GameId { get; private set; }
	public string Name { get; private set; } = string.Empty;
	public string NormalizedName { get; private set; } = string.Empty;
	public int? Width { get; private set; }
	public int? Height { get; private set; }
	public long LastCreationSequence { get; private set; }
	public DateTimeOffset CreatedAt { get; private set; }
	public DateTimeOffset UpdatedAt { get; private set; }
	public IReadOnlyCollection<MapStyle> Styles => styles.AsReadOnly();
	public MapGeometry? Geometry => geometry ??= Width is { } width && Height is { } height
		? new MapGeometry(width, height) : null;

	public Guid? DefaultStyleId { get; private set; }

	public static string NormalizeName(string name) => name.Trim().ToUpperInvariant();

	public static Result<Map> Create(Guid gameId, string? name)
	{
		name = name?.Trim();

		if (string.IsNullOrEmpty(name) || name.Length > NameMaxLength)
			return MapErrors.InvalidMapName;

		var now = DateTimeOffset.UtcNow;
		var map = new Map(Guid.CreateVersion7())
		{
			GameId = gameId,
			Name = name,
			NormalizedName = NormalizeName(name),
			CreatedAt = now,
			UpdatedAt = now
		};

		return map;
	}

	public Result<bool> Rename(string? name)
	{
		name = name?.Trim();

		if (string.IsNullOrEmpty(name) || name.Length > NameMaxLength)
			return MapErrors.InvalidMapName;

		if (name == Name)
			return false;

		Name = name;
		NormalizedName = NormalizeName(name);
		UpdatedAt = DateTimeOffset.UtcNow;

		return true;
	}

	public Result<MapStyle> AddStyle(MapStyleInput input)
	{
		if (input.Source is null || !input.Source.IsRaster)
			return MapProcessingErrors.InvalidSource;

		var created = MapStyle.Create(Id, LastCreationSequence + 1, input.Name, input.BackgroundColor,
			input.Source);

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

		var sourceGeometry = MapGeometry.Create(style.Width, style.Height);

		if (sourceGeometry.IsFailure)
			return sourceGeometry.Errors;

		if (styles.Any(existing => existing.NormalizedName == style.NormalizedName))
			return MapErrors.NameConflict;

		style.AssignCreationSequence(++LastCreationSequence);
		style.AssignDisplayOrder(styles.Count == 0 ? 0 : styles.Max(item => item.DisplayOrder) + 1);
		Width ??= style.Width;
		Height ??= style.Height;
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
		ReconcileDefaultStyle();
		UpdatedAt = DateTimeOffset.UtcNow;

		return Result.Result.Success();
	}

	public void ReconcileDefaultStyle()
	{
		if (styles.Any(style => style.Id == DefaultStyleId && style.Status == MapProcessingStatus.Ready))
			return;

		DefaultStyleId = styles.Where(style => style.Status == MapProcessingStatus.Ready)
			.OrderBy(style => style.DisplayOrder).ThenBy(style => style.CreationSequence)
			.Select(style => (Guid?)style.Id).FirstOrDefault();
	}

	public Result<bool> Update(Optional<string?> name = default, Optional<Guid?> defaultStyleId = default,
		Optional<IReadOnlyList<Guid>?> styleOrder = default)
	{
		var nextName = name.GetValueOrDefault(Name)?.Trim();

		if (string.IsNullOrEmpty(nextName) || nextName.Length > NameMaxLength)
			return MapErrors.InvalidMapName;

		if (defaultStyleId.IsSpecified)
		{
			if (defaultStyleId.Value is not { } selectedId)
				return MapErrors.InvalidDefaultStyle;

			var selected = styles.SingleOrDefault(style => style.Id == selectedId);

			if (selected is null)
				return MapErrors.MissingStyle;

			if (selected.Status != MapProcessingStatus.Ready)
				return MapErrors.InvalidDefaultStyle;
		}

		if (styleOrder.IsSpecified)
		{
			var order = styleOrder.Value;

			if (order is null || order.Distinct().Count() != order.Count)
				return MapErrors.InvalidStyleOrder;

			if (order.Any(id => styles.All(style => style.Id != id)))
				return MapErrors.MissingStyle;

			if (order.Count != styles.Count)
				return MapErrors.InvalidStyleOrder;
		}

		var changed = nextName != Name || defaultStyleId.GetValueOrDefault(DefaultStyleId) != DefaultStyleId;

		if (styleOrder.IsSpecified)
		{
			var order = styleOrder.Value!;
			changed |= !styles.OrderBy(style => style.DisplayOrder).Select(style => style.Id).SequenceEqual(order);

			if (changed)
			{
				for (var index = 0; index < order.Count; index++)
					styles.Single(style => style.Id == order[index]).AssignDisplayOrder(index);
			}
		}

		if (!changed)
			return false;

		Name = nextName;
		NormalizedName = NormalizeName(nextName);
		DefaultStyleId = defaultStyleId.GetValueOrDefault(DefaultStyleId);
		UpdatedAt = DateTimeOffset.UtcNow;

		return true;
	}
}
