using GameAtlas.Application.Abstractions.Persistence;
using Microsoft.AspNetCore.Diagnostics;

namespace GameAtlas.Api.Errors;

public sealed class GameSlugConflictExceptionHandler(
	IEnumerable<IPersistenceFailureClassifier> classifiers) : IExceptionHandler
{
	public async ValueTask<bool> TryHandleAsync(
		HttpContext context, Exception exception, CancellationToken cancellationToken)
	{
		if (!classifiers.Any(classifier => classifier.IsGameSlugConflict(exception)))
			return false;

		await TypedResults.Problem(statusCode: 409, title: "Conflict",
			detail: "A game with this slug already exists.",
			extensions: new Dictionary<string, object?>
			{
				["errors"] = new Dictionary<string, string[]>
				{
					["slug"] = ["A game with this slug already exists."]
				}
			}).ExecuteAsync(context);

		return true;
	}
}
