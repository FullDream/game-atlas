using System.Text.RegularExpressions;
using GameAtlas.Domain.Media;
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
	public Guid CoverId { get; private set; }
	public ImageAsset Cover { get; private set; } = null!;
	public Guid? BannerId { get; private set; }
	public ImageAsset? Banner { get; private set; }
	public DateOnly? ReleaseDate { get; private set; }
	public string? Developer { get; private set; }
	public string? Description { get; private set; }
	public DateTimeOffset CreatedAt { get; private set; }
	public DateTimeOffset UpdatedAt { get; private set; }
	public GameStatus Status { get; private set; }

	public static Result<Game> Create(string? name, string? slug, Guid? coverId, Guid? bannerId = null, DateOnly? releaseDate = null,
		string? developer = null, string? description = null)
	{
		name = name?.Trim();
		slug = slug?.Trim();
		var errors = new List<Error>();

		ValidateRequired(name, GameErrors.NameRequired, errors);
		ValidateRequired(slug, GameErrors.SlugRequired, errors);
		if (coverId is null || coverId == Guid.Empty)
			errors.Add(GameErrors.CoverRequired);

		if (!string.IsNullOrEmpty(slug) && !SlugPattern.IsMatch(slug))
			errors.Add(GameErrors.InvalidSlug);

		if (errors.Count > 0)
			return errors.ToArray();

		var now = DateTimeOffset.UtcNow;

		return new Game(Guid.CreateVersion7())
		{
			Name = name!,
			Slug = slug!,
			CoverId = coverId!.Value,
			BannerId = bannerId,
			ReleaseDate = releaseDate,
			Developer = NormalizeDeveloper(developer),
			Description = string.IsNullOrEmpty(description) ? null : description,
			CreatedAt = now,
			UpdatedAt = now,
			Status = GameStatus.Draft
		};
	}

	public Result<bool> Update(
		Optional<string?> name = default,
		Optional<string?> slug = default,
		Optional<Guid?> coverId = default,
		Optional<GameStatus?> status = default,
		Optional<Guid?> bannerId = default,
		Optional<DateOnly?> releaseDate = default,
		Optional<string?> developer = default,
		Optional<string?> description = default)
	{
		var nextName = name.GetValueOrDefault(Name)?.Trim();
		var nextSlug = slug.GetValueOrDefault(Slug)?.Trim();
		var nextCover = coverId.GetValueOrDefault(CoverId);
		var nextStatus = status.GetValueOrDefault(Status);
		var nextBanner = bannerId.GetValueOrDefault(BannerId);
		var nextRelease = releaseDate.GetValueOrDefault(ReleaseDate);
		var nextDeveloper = NormalizeDeveloper(developer.GetValueOrDefault(Developer));
		var nextDescription = description.GetValueOrDefault(Description);

		if (string.IsNullOrEmpty(nextDescription))
			nextDescription = null;

		var errors = new List<Error>();

		ValidateRequired(nextName, GameErrors.NameRequired, errors);
		ValidateRequired(nextSlug, GameErrors.SlugRequired, errors);
		if (nextCover is null || nextCover == Guid.Empty)
			errors.Add(GameErrors.CoverRequired);

		if (!string.IsNullOrEmpty(nextSlug) && !SlugPattern.IsMatch(nextSlug))
			errors.Add(GameErrors.InvalidSlug);

		if (nextStatus is null)
			errors.Add(GameErrors.StatusRequired);
		else if (!Enum.IsDefined(nextStatus.Value))
			errors.Add(GameErrors.InvalidStatus);

		if (errors.Count > 0 || nextName is null || nextSlug is null || nextCover is null || nextStatus is null)
			return errors.ToArray();

		if (nextName == Name && nextSlug == Slug && nextCover == CoverId && nextStatus == Status &&
			nextBanner == BannerId && nextRelease == ReleaseDate && nextDeveloper == Developer && nextDescription == Description)
			return false;

		Name = nextName;
		Slug = nextSlug;
		CoverId = nextCover.Value;
		Status = nextStatus.Value;
		BannerId = nextBanner;
		ReleaseDate = nextRelease;
		Developer = nextDeveloper;
		Description = nextDescription;
		UpdatedAt = DateTimeOffset.UtcNow;

		return true;
	}

	private static string? NormalizeDeveloper(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

	private static void ValidateRequired(string? value, Error error, List<Error> errors)
	{
		if (string.IsNullOrEmpty(value))
			errors.Add(error);
	}
}
