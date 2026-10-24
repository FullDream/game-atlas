using GameAtlas.Application.Categories.CreateCategory;
using GameAtlas.Application.Categories.UpdateCategory;

namespace GameAtlas.Application.Tests;

public class CategoryValidationTests
{
	[Theory]
	[InlineData(0, false)]
	[InlineData(1, true)]
	[InlineData(200, true)]
	[InlineData(201, false)]
	public void NameBoundsApplyAfterTrimming(int length, bool valid)
	{
		var name = " " + new string('x', length) + " ";

		Assert.Equal(valid, new CreateCategoryValidation().Validate(Create(name: name)).IsValid);
		Assert.Equal(valid, new UpdateCategoryValidation().Validate(Update() with { Name = name }).IsValid);
	}

	[Theory]
	[InlineData(0, false)]
	[InlineData(1, true)]
	[InlineData(2048, true)]
	[InlineData(2049, false)]
	public void IconBoundsApplyAfterTrimming(int length, bool valid)
	{
		var icon = " " + new string('x', length) + " ";

		Assert.Equal(valid, new CreateCategoryValidation().Validate(Create(icon: icon)).IsValid);
		Assert.Equal(valid, new UpdateCategoryValidation().Validate(Update() with { Icon = icon }).IsValid);
	}

	[Theory]
	[InlineData(null, false)]
	[InlineData("", false)]
	[InlineData("red", false)]
	[InlineData("#F80", true)]
	[InlineData("#fff", true)]
	[InlineData("#aBc", true)]
	[InlineData("#12", false)]
	[InlineData("#1234", false)]
	[InlineData("#12345", false)]
	[InlineData("#123\n", false)]
	[InlineData("#12G", false)]
	[InlineData("#FF880080", false)]
	[InlineData(" #FF8800", false)]
	[InlineData("#FF8800\n", false)]
	[InlineData("#GG8800", false)]
	[InlineData("#ff8800", true)]
	[InlineData("#FF8800", true)]
	public void ColorRequiresExactHexFormat(string? color, bool valid)
	{
		Assert.Equal(valid, new CreateCategoryValidation().Validate(Create(color: color)).IsValid);
		Assert.Equal(valid, new UpdateCategoryValidation().Validate(Update() with { Color = color }).IsValid);
	}

	[Theory]
	[InlineData(null, false)]
	[InlineData(0, false)]
	[InlineData(-1, false)]
	[InlineData(1, true)]
	[InlineData(int.MaxValue, true)]
	public void OptionalOrderValidatesSuppliedValue(int? order, bool valid)
	{
		Assert.Equal(valid, new CreateCategoryValidation().Validate(Create() with { Order = order }).IsValid);
		Assert.Equal(valid, new UpdateCategoryValidation().Validate(Update() with { Order = order }).IsValid);
		Assert.True(new CreateCategoryValidation().Validate(Create()).IsValid);
	}

	[Fact]
	public void RejectsEmptyPatchAndNullRequiredFieldsButAllowsGuidLookupMiss()
	{
		var create = new CreateCategoryValidation();
		var update = new UpdateCategoryValidation();

		Assert.False(create.Validate(Create(name: null)).IsValid);
		Assert.False(create.Validate(Create(icon: null)).IsValid);
		Assert.False(update.Validate(Update()).IsValid);
		Assert.False(update.Validate(Update() with { Name = (string?)null }).IsValid);
		Assert.False(update.Validate(Update() with { Icon = (string?)null }).IsValid);
		Assert.False(update.Validate(Update() with { Color = (string?)null }).IsValid);
		Assert.False(update.Validate(Update() with { CategoryGroupId = (Guid?)null }).IsValid);
		Assert.True(update.Validate(Update() with { CategoryGroupId = Guid.Empty }).IsValid);
	}

	private static CreateCategory Create(string? name = "Name", string? icon = "icon", string? color = "#123456") =>
		new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), name, icon, color);

	private static UpdateCategory Update() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
}
