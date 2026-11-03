using System.Text.RegularExpressions;
using GameAtlas.Domain.Result;

namespace GameAtlas.Domain.Categories;

public sealed class Category
{
	public const string ColorPattern = @"^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6})(?![\s\S])";
	private static readonly Regex ColorRegex = new(ColorPattern, RegexOptions.CultureInvariant);

	private Category(Guid id) => Id = id;

	public Guid Id { get; }
	public Guid CategoryGroupId { get; private set; }
	public string Name { get; private set; } = string.Empty;
	public string NormalizedName { get; private set; } = string.Empty;
	public Guid? IconAssetId { get; private set; }
	public string Color { get; private set; } = string.Empty;
	public int Order { get; private set; }
	public DateTimeOffset CreatedAt { get; private set; }
	public DateTimeOffset UpdatedAt { get; private set; }

	public static string NormalizeName(string name) => name.Trim().ToUpperInvariant();

	public static Result<Category> Create(Guid categoryGroupId, string? name, Guid? iconAssetId, string? color, int order)
	{
		var category = new Category(Guid.CreateVersion7());

		var result = category.Update(name, iconAssetId, color, order, categoryGroupId);

		if (result.IsFailure)
			return result.Errors;

		category.CreatedAt = category.UpdatedAt;

		return category;
	}

	public Result<bool> Update(Optional<string?> name = default, Optional<Guid?> iconAssetId = default,
		Optional<string?> color = default, Optional<int?> order = default, Optional<Guid?> categoryGroupId = default)
	{
		var nextName = name.GetValueOrDefault(Name)?.Trim();

		var nextIcon = iconAssetId.GetValueOrDefault(IconAssetId);

		var nextColor = color.GetValueOrDefault(Color);

		var nextOrder = order.GetValueOrDefault(Order);

		var nextGroupId = categoryGroupId.GetValueOrDefault(CategoryGroupId);

		if (string.IsNullOrEmpty(nextName))
			return CategoryErrors.NameRequired;


		if (nextColor is null || !ColorRegex.IsMatch(nextColor))
			return CategoryErrors.InvalidColor;

		if (nextOrder is null or <= 0)
			return CategoryErrors.InvalidOrder;

		if (nextGroupId is null)
			return CategoryErrors.GroupRequired;

		nextColor = NormalizeColor(nextColor);

		if (nextName == Name && nextIcon == IconAssetId && nextColor == Color && nextOrder == Order &&
			nextGroupId == CategoryGroupId)
			return false;

		Name = nextName;
		NormalizedName = NormalizeName(nextName);
		IconAssetId = nextIcon;
		Color = nextColor;
		Order = nextOrder.Value;
		CategoryGroupId = nextGroupId.Value;
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

	internal static string NormalizeColor(string color)
	{
		color = color.ToUpperInvariant();

		if (color.Length == 7)
			return color;

		return string.Create(7, color, static (characters, shortColor) =>
		{
			characters[0] = '#';
			characters[1] = characters[2] = shortColor[1];
			characters[3] = characters[4] = shortColor[2];
			characters[5] = characters[6] = shortColor[3];
		});
	}
}
