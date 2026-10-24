using GameAtlas.Domain.Result;

namespace GameAtlas.Application.Media.Storage;

public static class MediaStorageErrors
{
	public static readonly Error ByteLimit = new(
		"Media.File.TooLarge", "The file exceeds the configured byte limit.", "File", ErrorType.Validation);
}
