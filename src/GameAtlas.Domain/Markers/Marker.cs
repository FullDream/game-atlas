using GameAtlas.Domain.Result;

namespace GameAtlas.Domain.Markers;

public sealed class Marker
{
	private readonly List<MarkerAsset> media = [];

	private Marker(Guid id) => Id = id;

	public Guid Id { get; }
	public Guid CategoryId { get; private set; }
	public string Name { get; private set; } = string.Empty;
	public string? Description { get; private set; }
	public Position Position { get; private set; } = null!;
	public DateTimeOffset CreatedAt { get; private set; }
	public DateTimeOffset UpdatedAt { get; private set; }
	public IReadOnlyCollection<MarkerAsset> Media => media.AsReadOnly();

	public static Result<Marker> Create(Guid categoryId, string? name, Position? position,
		string? description = null, IReadOnlyCollection<Guid?>? media = null)
	{
		var marker = new Marker(Guid.CreateVersion7());
		var result = marker.Update(categoryId, name, description, position,
			Optional<IReadOnlyCollection<Guid?>?>.From(media ?? []));

		if (result.IsFailure)
			return result.Errors;

		marker.CreatedAt = marker.UpdatedAt;

		return marker;
	}

	public Result<bool> Update(Optional<Guid?> categoryId = default, Optional<string?> name = default,
		Optional<string?> description = default, Optional<Position?> position = default,
		Optional<IReadOnlyCollection<Guid?>?> media = default)
	{
		var nextCategoryId = categoryId.GetValueOrDefault(CategoryId);
		var nextName = name.GetValueOrDefault(Name)?.Trim();
		var nextDescription = description.GetValueOrDefault(Description)?.Trim();
		var nextPosition = position.GetValueOrDefault(Position);
		var nextMedia = media.GetValueOrDefault(this.media.Select(item => (Guid?)item.AssetId).ToArray())?.ToArray();

		if (nextCategoryId is null)
			return MarkerErrors.CategoryRequired;

		if (string.IsNullOrEmpty(nextName) || nextName.Length > MarkerLimits.NameMaxLength)
			return MarkerErrors.InvalidName;

		if (nextDescription?.Length > MarkerLimits.DescriptionMaxLength)
			return MarkerErrors.InvalidDescription;

		if (nextPosition is null)
			return MarkerErrors.PositionRequired;

		if (nextMedia is null || nextMedia.Any(id => id is null || id == Guid.Empty) ||
			nextMedia.Distinct().Count() != nextMedia.Length)
			return MarkerErrors.InvalidMedia;

		nextDescription = string.IsNullOrEmpty(nextDescription) ? null : nextDescription;
		var assetIds = nextMedia.Select(id => id.GetValueOrDefault()).ToArray();
		var sameMedia = this.media.OrderBy(item => item.Order).Select(item => item.AssetId).SequenceEqual(assetIds);

		var hasChanges = nextCategoryId != CategoryId
			|| nextName != Name
			|| nextDescription != Description
			|| nextPosition != Position
			|| !sameMedia;

		if (!hasChanges)
			return false;

		CategoryId = nextCategoryId.Value;
		Name = nextName;
		Description = nextDescription;
		Position = nextPosition;

		if (!sameMedia)
		{
			var retained = this.media.ToDictionary(item => item.AssetId);

			this.media.Clear();

			foreach (var (index, assetId) in assetIds.Index())
			{
				var attachment = retained.GetValueOrDefault(assetId) ?? new MarkerAsset(Id, assetId, index);

				attachment.SetOrder(index);
				this.media.Add(attachment);
			}
		}

		UpdatedAt = DateTimeOffset.UtcNow;

		return true;
	}

}
