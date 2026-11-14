using GameAtlas.Domain.Result;

namespace GameAtlas.Domain.Categories;

public sealed class CategoryGroup
{
	private CategoryGroup(Guid id, Guid mapId)
	{
		Id = id;
		MapId = mapId;
	}

	public Guid Id { get; }
	public Guid MapId { get; }
	public string Name { get; private set; } = string.Empty;
	public string NormalizedName { get; private set; } = string.Empty;
	public string Color { get; private set; } = "#F59E0B";
	public PublicationStatus Status { get; private set; }
	public int Order { get; private set; }
	public Guid? IconAssetId { get; private set; }
	public DateTimeOffset CreatedAt { get; private set; }
	public DateTimeOffset UpdatedAt { get; private set; }

	public static string NormalizeName(string name) => name.Trim().ToUpperInvariant();

	public static Result<CategoryGroup> Create(
		Guid mapId,
		string? name,
		int order,
		Guid? iconAssetId = null,
		string? color = "#F59E0B",
		PublicationStatus status = PublicationStatus.Draft)
	{
		name = name?.Trim();

		if (string.IsNullOrEmpty(name))
			return CategoryGroupErrors.NameRequired;

		if (color is null || !System.Text.RegularExpressions.Regex.IsMatch(color, Category.ColorPattern))
			return CategoryGroupErrors.InvalidColor;

		if (order <= 0)
			return CategoryGroupErrors.InvalidOrder;

		if (!Enum.IsDefined(status))
			return CategoryGroupErrors.InvalidStatus;

		var now = DateTimeOffset.UtcNow;

		return new CategoryGroup(Guid.CreateVersion7(), mapId)
		{
			Name = name,
			Color = Category.NormalizeColor(color!),
			Status = status,
			NormalizedName = NormalizeName(name),
			Order = order,
			IconAssetId = iconAssetId,
			CreatedAt = now,
			UpdatedAt = now
		};
	}

	public Result<bool> Update(
		Optional<string?> name = default,
		Optional<int?> order = default,
		Optional<Guid?> iconAssetId = default,
		Optional<string?> color = default,
		Optional<PublicationStatus?> status = default)
	{
		var nextName = name.GetValueOrDefault(Name)?.Trim();

		var nextColor = color.GetValueOrDefault(Color);

		if (nextColor is null || !System.Text.RegularExpressions.Regex.IsMatch(nextColor, Category.ColorPattern))
			return CategoryGroupErrors.InvalidColor;

		nextColor = Category.NormalizeColor(nextColor);

		var nextStatus = status.GetValueOrDefault(Status);

		if (nextStatus is null)
			return CategoryGroupErrors.StatusRequired;

		if (!Enum.IsDefined(nextStatus.Value))
			return CategoryGroupErrors.InvalidStatus;

		var nextOrder = order.GetValueOrDefault(Order);

		var nextIcon = iconAssetId.GetValueOrDefault(IconAssetId);

		if (string.IsNullOrEmpty(nextName))
			return CategoryGroupErrors.NameRequired;

		if (nextOrder is null or <= 0)
			return CategoryGroupErrors.InvalidOrder;

		if (nextName == Name && nextOrder == Order && nextIcon == IconAssetId && nextColor == Color &&
			nextStatus.Value == Status)
			return false;

		Name = nextName;
		NormalizedName = NormalizeName(nextName);
		Order = nextOrder.Value;
		IconAssetId = nextIcon;
		Color = nextColor;
		Status = nextStatus.Value;
		UpdatedAt = DateTimeOffset.UtcNow;

		return true;
	}

	public void PlaceAt(int order)
	{
		SetDisplayOrder(order);
		UpdatedAt = DateTimeOffset.UtcNow;
	}

	public void SetDisplayOrder(int order)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(order);
		Order = order;
	}

	public bool Publish()
	{
		if (Status == PublicationStatus.Published)
			return false;

		Status = PublicationStatus.Published;
		UpdatedAt = DateTimeOffset.UtcNow;

		return true;
	}
}
