using GameAtlas.Domain.Markers.Media;
using GameAtlas.Domain.Result;

namespace GameAtlas.Domain.Markers;

public sealed class Marker
{
	private readonly List<MarkerMedia> media = [];

	private Marker(Guid id) => Id = id;

	public Guid Id { get; }
	public Guid CategoryId { get; private set; }
	public string Name { get; private set; } = string.Empty;
	public string? Description { get; private set; }
	public Position Position { get; private set; } = null!;
	public DateTimeOffset CreatedAt { get; private set; }
	public DateTimeOffset UpdatedAt { get; private set; }
	public IReadOnlyCollection<MarkerMedia> Media => media.AsReadOnly();

	public static Result<Marker> Create(Guid categoryId, string? name, Position? position,
		string? description = null, IReadOnlyCollection<MarkerMedia?>? media = null)
	{
		var marker = new Marker(Guid.CreateVersion7());
		var result = marker.Update(categoryId, name, description, position,
			Optional<IReadOnlyCollection<MarkerMedia?>?>.From(media ?? []));

		if (result.IsFailure)
			return result.Errors;

		marker.CreatedAt = marker.UpdatedAt;

		return marker;
	}

	public Result<bool> Update(Optional<Guid?> categoryId = default, Optional<string?> name = default,
		Optional<string?> description = default, Optional<Position?> position = default,
		Optional<IReadOnlyCollection<MarkerMedia?>?> media = default)
	{
		var nextCategoryId = categoryId.GetValueOrDefault(CategoryId);
		var nextName = name.GetValueOrDefault(Name)?.Trim();
		var nextDescription = description.GetValueOrDefault(Description)?.Trim();
		var nextPosition = position.GetValueOrDefault(Position);
		var nextMedia = media.GetValueOrDefault(this.media)?.ToArray();

		if (nextCategoryId is null)
			return MarkerErrors.CategoryRequired;

		if (string.IsNullOrEmpty(nextName) || nextName.Length > MarkerLimits.NameMaxLength)
			return MarkerErrors.InvalidName;

		if (nextDescription?.Length > MarkerLimits.DescriptionMaxLength)
			return MarkerErrors.InvalidDescription;

		if (nextPosition is null)
			return MarkerErrors.PositionRequired;

		if (nextMedia is null || !HasValidMedia(nextMedia))
			return MarkerErrors.InvalidMedia;

		nextDescription = string.IsNullOrEmpty(nextDescription) ? null : nextDescription;
		var attachments = nextMedia.OfType<MarkerMedia>().ToArray();
		var sameMedia = this.media.Count == attachments.Length && this.media
			.Zip(attachments).All(pair => pair.First.HasSameContent(pair.Second));

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
			this.media.Clear();

			foreach (var (index, attachment) in attachments.Index())
				attachment.AttachTo(Id, index);

			this.media.AddRange(attachments);
		}

		UpdatedAt = DateTimeOffset.UtcNow;

		return true;
	}

	private bool HasValidMedia(IReadOnlyCollection<MarkerMedia?> attachments)
	{
		var ids = new HashSet<Guid>();

		foreach (var attachment in attachments)
		{
			if (attachment is null)
				return false;

			if (attachment.MarkerId != Guid.Empty && attachment.MarkerId != Id)
				return false;

			if (!ids.Add(attachment.Id))
				return false;
		}

		return true;
	}
}
