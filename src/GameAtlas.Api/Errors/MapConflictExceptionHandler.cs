using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Application.Maps;
using GameAtlas.Domain.Maps;
using Microsoft.AspNetCore.Diagnostics;

namespace GameAtlas.Api.Errors;

public sealed class MapConflictExceptionHandler(IEnumerable<IPersistenceFailureClassifier> classifiers)
	: IExceptionHandler
{
	public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception,
		CancellationToken cancellationToken)
	{
		var mapConflict = classifiers.Any(classifier => classifier.IsMapNameConflict(exception));
		var nameConflict = classifiers.Any(classifier => classifier.IsMapStyleNameConflict(exception));

		if (!mapConflict && !nameConflict)
			return false;

		var error = mapConflict ? MapOperationErrors.NameConflict : MapErrors.NameConflict;

		await TypedResults.Problem(statusCode: 409, title: "Conflict", detail: error.Message).ExecuteAsync(context);

		return true;
	}
}
