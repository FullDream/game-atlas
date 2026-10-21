using System.Text.Json;
using GameAtlas.Api.Serialization.Optional;
using GameAtlas.Api.Results;
using GameAtlas.Domain;
using GameAtlas.Domain.Result;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using DomainResult = GameAtlas.Domain.Result.Result;

namespace GameAtlas.IntegrationTests;

public class OptionalAndResultTests
{
	[Theory]
	[InlineData("{}", false, null)]
	[InlineData("{\"Name\":null}", true, null)]
	[InlineData("{\"Name\":\"Atlas\"}", true, "Atlas")]
	public void OptionalJsonDistinguishesMissingNullAndValue(string json, bool specified, string? value)
	{
		var options = new JsonSerializerOptions().AddOptionalSupport();
		var payload = JsonSerializer.Deserialize<UpdatePayload>(json, options)!;

		Assert.Equal(specified, payload.Name.IsSpecified);

		if (specified)
			Assert.Equal(value, payload.Name.Value);

		Assert.Equal(json, JsonSerializer.Serialize(payload, options));
	}

	[Theory]
	[InlineData(ErrorType.Validation, 422)]
	[InlineData(ErrorType.NotFound, 404)]
	[InlineData(ErrorType.Conflict, 409)]
	public async Task OutcomeResultMapsFailuresToHttpStatuses(ErrorType errorType, int expectedStatus)
	{
		var services = new ServiceCollection();

		services.AddLogging();
		await using var provider = services.BuildServiceProvider();
		var context = new DefaultHttpContext { RequestServices = provider };

		context.Response.Body = new MemoryStream();
		var result = new OutcomeResult(DomainResult.Failure(new Error("test", "Test error", Type: errorType)));

		await result.ExecuteAsync(context);

		Assert.Equal(expectedStatus, context.Response.StatusCode);
		Assert.StartsWith("application/problem+json", context.Response.ContentType);
	}

	public sealed record UpdatePayload(Optional<string?> Name);
}
