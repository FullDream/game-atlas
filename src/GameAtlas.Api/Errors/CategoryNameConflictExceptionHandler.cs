using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Application.Categories.CreateCategory;
using Microsoft.AspNetCore.Diagnostics;

namespace GameAtlas.Api.Errors;

public sealed class CategoryNameConflictExceptionHandler(
	IEnumerable<IPersistenceFailureClassifier> classifiers) : IExceptionHandler
{
	public async ValueTask<bool> TryHandleAsync(
		HttpContext context, Exception exception, CancellationToken cancellationToken)
	{
		if (!classifiers.Any(classifier => classifier.IsCategoryNameConflict(exception)))
			return false;

		var message = CreateCategoryErrors.NameConflict.Message;

		await TypedResults.Problem(statusCode: 409, title: "Conflict", detail: message,
			extensions: new Dictionary<string, object?>
			{
				["errors"] = new Dictionary<string, string[]> { ["name"] = [message] }
			}).ExecuteAsync(context);

		return true;
	}
}
