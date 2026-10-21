using System.Text.Json;
using FluentValidation;
using GameAtlas.Api;
using GameAtlas.Api.OpenApi.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class OpenApiValidationTests
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task OpenApiIntersectsRangesAndPreservesZeroMaxLength(bool reverseOrder)
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });

		builder.WebHost.UseUrls("http://127.0.0.1:0");
		builder.Services.AddScoped<IValidator<RangeContract>>(_ => new RangeContractValidator(reverseOrder));
		builder.Services.AddApi();
		await using var app = builder.Build();

		app.MapOpenApi();
		app.MapPost("/ranges", (RangeContract request) => request);
		await app.StartAsync();
		using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
		using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
		var properties = document.RootElement.GetProperty("components").GetProperty("schemas")
			.EnumerateObject().Single(property => property.Name.EndsWith(nameof(RangeContract))).Value
			.GetProperty("properties");

		Assert.Equal(5, properties.GetProperty("inclusive").GetProperty("minimum").GetInt32());
		Assert.Equal(10, properties.GetProperty("inclusive").GetProperty("maximum").GetInt32());
		Assert.Equal(5, properties.GetProperty("exclusive").GetProperty("exclusiveMinimum").GetInt32());
		Assert.Equal(10, properties.GetProperty("exclusive").GetProperty("exclusiveMaximum").GetInt32());
		Assert.False(properties.GetProperty("mixed").TryGetProperty("minimum", out _));
		Assert.Equal(10, properties.GetProperty("mixed").GetProperty("maximum").GetInt32());
		Assert.Equal(5, properties.GetProperty("mixed").GetProperty("exclusiveMinimum").GetInt32());
		Assert.False(properties.GetProperty("mixed").TryGetProperty("exclusiveMaximum", out _));
		Assert.Equal(5, properties.GetProperty("tied").GetProperty("exclusiveMinimum").GetInt32());
		Assert.Equal(10, properties.GetProperty("tied").GetProperty("exclusiveMaximum").GetInt32());
		Assert.False(properties.GetProperty("tied").TryGetProperty("minimum", out _));
		Assert.False(properties.GetProperty("tied").TryGetProperty("maximum", out _));
		Assert.Equal(0, properties.GetProperty("empty").GetProperty("maxLength").GetInt32());
		Assert.Equal("email", properties.GetProperty("email").GetProperty("format").GetString());
	}

	[Fact]
	public async Task OpenApiUsesRulesFromMappedScopedValidator()
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });

		builder.WebHost.UseUrls("http://127.0.0.1:0");
		builder.Services.AddValidatorsFromAssemblyContaining<ValidationModelValidator>();
		builder.Services.AddApi();
		builder.Services.AddOpenApiValidationSource<RequestContract, ValidationModel>();
		await using var app = builder.Build();

		app.MapOpenApi();
		app.MapPost("/requests", (RequestContract request) => request);
		await app.StartAsync();
		using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
		using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
		var requestSchema = document.RootElement.GetProperty("components").GetProperty("schemas")
			.EnumerateObject().Single(property => property.Name.EndsWith(nameof(RequestContract))).Value;
		var properties = requestSchema.GetProperty("properties");

		Assert.Equal(3, properties.GetProperty("name").GetProperty("minLength").GetInt32());
		Assert.Equal(20, properties.GetProperty("name").GetProperty("maxLength").GetInt32());
		Assert.Equal(1, properties.GetProperty("count").GetProperty("minimum").GetInt32());
		Assert.Equal(10, properties.GetProperty("count").GetProperty("maximum").GetInt32());
	}

	[Fact]
	public async Task RegisteredValidatorRejectsInvalidInput()
	{
		var services = new ServiceCollection();

		services.AddValidatorsFromAssemblyContaining<ValidationModelValidator>();
		await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
		await using var scope = provider.CreateAsyncScope();
		var validator = scope.ServiceProvider.GetRequiredService<IValidator<ValidationModel>>();
		var result = await validator.ValidateAsync(new ValidationModel("a", 0));

		Assert.False(result.IsValid);
		Assert.Contains(result.Errors, error => error.PropertyName == nameof(ValidationModel.Name));
		Assert.Contains(result.Errors, error => error.PropertyName == nameof(ValidationModel.Count));
	}

	public sealed record RequestContract(string Name, int Count);
	public sealed record ValidationModel(string Name, int Count);
	public sealed record RangeContract(int Inclusive, int Exclusive, int Mixed, int Tied, string Empty, string Email);

	private sealed class RangeContractValidator : AbstractValidator<RangeContract>
	{
		public RangeContractValidator(bool reverseOrder)
		{
			foreach (var (from, to) in reverseOrder ? new[] { (5, 20), (1, 10) } : [(1, 10), (5, 20)])
			{
				RuleFor(model => model.Inclusive).InclusiveBetween(from, to);
				RuleFor(model => model.Exclusive).ExclusiveBetween(from, to);

				if (from == 1)
				{
					RuleFor(model => model.Mixed).InclusiveBetween(from, to);
					RuleFor(model => model.Tied).InclusiveBetween(5, 10);
				}
				else
				{
					RuleFor(model => model.Mixed).ExclusiveBetween(from, to);
					RuleFor(model => model.Tied).ExclusiveBetween(5, 10);
				}
			}

			RuleFor(model => model.Empty).MaximumLength(0);
			RuleFor(model => model.Empty).MaximumLength(10);
			RuleFor(model => model.Email).EmailAddress();
		}
	}

	public sealed class ValidationModelValidator : AbstractValidator<ValidationModel>
	{
		public ValidationModelValidator()
		{
			RuleFor(model => model.Name).NotEmpty().Length(3, 20);
			RuleFor(model => model.Count).InclusiveBetween(1, 10);
		}
	}
}
