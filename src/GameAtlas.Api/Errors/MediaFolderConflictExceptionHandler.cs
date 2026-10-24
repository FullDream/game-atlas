using GameAtlas.Api.Results;
using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Application.Media;
using Microsoft.AspNetCore.Diagnostics;

namespace GameAtlas.Api.Errors;

public sealed class MediaFolderConflictExceptionHandler(IEnumerable<IPersistenceFailureClassifier> classifiers)
	: IExceptionHandler
{
	public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception,
		CancellationToken cancellationToken)
	{
		if (!classifiers.Any(classifier => classifier.IsMediaFolderNameConflict(exception)))
			return false;

		await new OutcomeResult(MediaOperationErrors.FolderNameConflict).ExecuteAsync(context);

		return true;
	}
}
