using GameAtlas.Domain.Media;
using GameAtlas.Domain.Result;
using GameAtlas.Domain.ValueObjects;

namespace GameAtlas.Domain.Games;

public sealed class Game
{
	private Game(Guid id) => Id = id;

	public Guid Id { get; }
	public string Name { get; private set; } = null!;
	public Slug Slug { get; private set; } = null!;
	public Guid CoverId { get; private set; }
	public ImageAsset Cover { get; private set; } = null!;
	public Guid? BannerId { get; private set; }
	public ImageAsset? Banner { get; private set; }
	public DateOnly? ReleaseDate { get; private set; }
	public string? Developer { get; private set; }
	public string? Description { get; private set; }
	public DateTimeOffset CreatedAt { get; private set; }
	public DateTimeOffset UpdatedAt { get; private set; }
	public PublicationStatus Status { get; private set; }

	public static Result<Game> Create(
		string? name,
		Slug slug,
		Guid? coverId,
		Guid? bannerId = null,
		DateOnly? releaseDate = null,
		string? developer = null,
		string? description = null)
	{
		name = name?.Trim();
		var errors = new List<Error>();

		ValidateRequired(name, GameErrors.NameRequired, errors);
		if (slug is null)
			errors.Add(GameErrors.SlugRequired);
		if (coverId is null || coverId == Guid.Empty)
			errors.Add(GameErrors.CoverRequired);


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
			Status = PublicationStatus.Draft
		};
	}

	public Result<bool> Update(
		Optional<string?> name = default,
		Optional<Slug?> slug = default,
		Optional<Guid?> coverId = default,
		Optional<PublicationStatus?> status = default,
		Optional<Guid?> bannerId = default,
		Optional<DateOnly?> releaseDate = default,
		Optional<string?> developer = default,
		Optional<string?> description = default)
	{
		var nextName = name.GetValueOrDefault(Name)?.Trim();
		var nextSlug = slug.GetValueOrDefault(Slug);
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
		if (nextSlug is null)
			errors.Add(GameErrors.SlugRequired);
		if (nextCover is null || nextCover == Guid.Empty)
			errors.Add(GameErrors.CoverRequired);


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
