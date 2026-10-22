using System.Text.RegularExpressions;
using GameAtlas.Domain.Result;

namespace GameAtlas.Domain.Games;

public sealed class Game
{
	private static readonly Regex SlugPattern = new(
		@"\A[a-z0-9]+(-[a-z0-9]+)*\z", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

	private Game(Guid id) => Id = id;

	public Guid Id { get; }
	public string Name { get; private set; } = null!;
	public string Slug { get; private set; } = null!;
	public string Image { get; private set; } = null!;
	public Guid? MapId { get; private set; }
	public DateTimeOffset CreatedAt { get; private set; }
	public DateTimeOffset UpdatedAt { get; private set; }
	public GameStatus Status { get; private set; }

	public static Result<Game> Create(string? name, string? slug, string? image, Guid? mapId = null)
	{
		name = name?.Trim();
		slug = slug?.Trim();
		image = image?.Trim();
		var errors = new List<Error>();

		ValidateRequired(name, GameErrors.NameRequired, errors);
		ValidateRequired(slug, GameErrors.SlugRequired, errors);
		ValidateRequired(image, GameErrors.ImageRequired, errors);

		if (!string.IsNullOrEmpty(slug) && !SlugPattern.IsMatch(slug))
			errors.Add(GameErrors.InvalidSlug);

		if (errors.Count > 0)
			return errors.ToArray();

		var now = DateTimeOffset.UtcNow;

		return new Game(Guid.CreateVersion7())
		{
			Name = name!,
			Slug = slug!,
			Image = image!,
			MapId = mapId,
			CreatedAt = now,
			UpdatedAt = now,
			Status = GameStatus.Draft
		};
	}

	public Result<bool> Update(
		Optional<string?> name = default,
		Optional<string?> slug = default,
		Optional<string?> image = default,
		Optional<Guid?> mapId = default,
		Optional<GameStatus?> status = default)
	{
		var nextName = name.GetValueOrDefault(Name)?.Trim();
		var nextSlug = slug.GetValueOrDefault(Slug)?.Trim();
		var nextImage = image.GetValueOrDefault(Image)?.Trim();
		var nextMapId = mapId.GetValueOrDefault(MapId);
		var nextStatus = status.GetValueOrDefault(Status);
		var errors = new List<Error>();

		ValidateRequired(nextName, GameErrors.NameRequired, errors);
		ValidateRequired(nextSlug, GameErrors.SlugRequired, errors);
		ValidateRequired(nextImage, GameErrors.ImageRequired, errors);

		if (!string.IsNullOrEmpty(nextSlug) && !SlugPattern.IsMatch(nextSlug))
			errors.Add(GameErrors.InvalidSlug);

		if (nextStatus is null)
			errors.Add(GameErrors.StatusRequired);
		else if (!Enum.IsDefined(nextStatus.Value))
			errors.Add(GameErrors.InvalidStatus);

		if (errors.Count > 0 || nextName is null || nextSlug is null || nextImage is null || nextStatus is null)
			return errors.ToArray();

		if (nextName == Name && nextSlug == Slug && nextImage == Image && nextMapId == MapId && nextStatus == Status)
			return false;

		Name = nextName;
		Slug = nextSlug;
		Image = nextImage;
		MapId = nextMapId;
		Status = nextStatus.Value;
		UpdatedAt = DateTimeOffset.UtcNow;

		return true;
	}

	private static void ValidateRequired(string? value, Error error, List<Error> errors)
	{
		if (string.IsNullOrEmpty(value))
			errors.Add(error);
	}
}
