using GameAtlas.Domain.Result;

namespace GameAtlas.Domain.Maps;

public static class MapProcessingErrors
{
	public static readonly Error ProcessingFailed = new(
		"Map.Style.ProcessingFailed", "The image could not be processed. Retry processing or create another style.");

	public static readonly Error RetryRequiresFailure = new(
		"Map.Style.Retry.Invalid", "Only failed processing can be retried.", Type: ErrorType.Conflict);

	public static readonly Error StaleAttempt = new(
		"Map.Style.Attempt.Stale", "The processing attempt is no longer current.", Type: ErrorType.Conflict);

	public static readonly Error InvalidSource = new(
		"Map.Style.Source.Invalid", "The source must be a nonempty PNG or JPEG with valid dimensions.",
		"File", ErrorType.Validation);
}
