using System.Text.RegularExpressions;
using GameAtlas.Domain.Result;

namespace GameAtlas.Domain.Maps;

public sealed class MapStyle
{
	public const int NameMaxLength = 200;
	public const string ColorPattern = @"\A#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6})\z";
	private static readonly Regex ColorRegex = new(ColorPattern, RegexOptions.CultureInvariant);

	private MapStyle(Guid id) => Id = id;

	public Guid Id { get; }
	public Guid MapId { get; private set; }
	public string Name { get; private set; } = string.Empty;
	public string NormalizedName { get; private set; } = string.Empty;
	public string BackgroundColor { get; private set; } = string.Empty;
	public long CreationSequence { get; private set; }
	public string Format { get; private set; } = string.Empty;
	public int Width { get; private set; }
	public int Height { get; private set; }
	public long ByteLength { get; private set; }
	public MapProcessingStatus Status { get; private set; }
	public int Progress { get; private set; }
	public string? ErrorCode { get; private set; }
	public string? ErrorMessage { get; private set; }
	public Guid AttemptId { get; private set; }
	public DateTimeOffset CreatedAt { get; private set; }
	public DateTimeOffset UpdatedAt { get; private set; }
	public static string NormalizeName(string name) => name.Trim().ToUpperInvariant();

	internal void AssignCreationSequence(long sequence) => CreationSequence = sequence;

	internal static Result<MapStyle> Create(Guid mapId, long sequence, string? name, string? color,
		string format, int width, int height, long byteLength)
	{
		var style = new MapStyle(Guid.CreateVersion7()) { MapId = mapId, CreationSequence = sequence };
		var metadata = style.UpdateMetadata(name, color);

		if (metadata.IsFailure)
			return metadata.Errors;

		if (format is not ("png" or "jpeg") || byteLength <= 0 || MapGeometry.Create(width, height).IsFailure)
			return MapProcessingErrors.InvalidSource;

		style.Format = format;
		style.Width = width;
		style.Height = height;
		style.ByteLength = byteLength;
		style.Status = MapProcessingStatus.Queued;
		style.AttemptId = Guid.CreateVersion7();
		style.CreatedAt = DateTimeOffset.UtcNow;
		style.UpdatedAt = style.CreatedAt;

		return style;
	}

	internal Result<bool> UpdateMetadata(Optional<string?> name = default, Optional<string?> backgroundColor = default)
	{
		var nextName = name.GetValueOrDefault(Name)?.Trim();
		var nextColor = backgroundColor.GetValueOrDefault(BackgroundColor);

		if (string.IsNullOrEmpty(nextName) || nextName.Length > NameMaxLength)
			return MapErrors.InvalidName;

		if (nextColor is null || !ColorRegex.IsMatch(nextColor))
			return MapErrors.InvalidColor;

		nextColor = NormalizeColor(nextColor);

		if (nextName == Name && nextColor == BackgroundColor)
			return false;

		Name = nextName;
		NormalizedName = NormalizeName(nextName);
		BackgroundColor = nextColor;
		UpdatedAt = DateTimeOffset.UtcNow;

		return true;
	}

	public Result.Result Claim(Guid attemptId)
	{
		if (Status != MapProcessingStatus.Queued || attemptId != AttemptId)
			return MapProcessingErrors.StaleAttempt;

		Status = MapProcessingStatus.Processing;
		UpdatedAt = DateTimeOffset.UtcNow;

		return Result.Result.Success();
	}

	public Result.Result ReportProgress(Guid attemptId, int progress)
	{
		if (!IsCurrentAttempt(attemptId))
			return MapProcessingErrors.StaleAttempt;

		var nextProgress = Math.Max(Progress, Math.Clamp(progress, 0, 99));

		if (nextProgress != Progress)
		{
			Progress = nextProgress;
			UpdatedAt = DateTimeOffset.UtcNow;
		}

		return Result.Result.Success();
	}

	public Result.Result Publish(Guid attemptId)
	{
		if (!IsCurrentAttempt(attemptId))
			return MapProcessingErrors.StaleAttempt;

		Status = MapProcessingStatus.Ready;
		Progress = 100;
		UpdatedAt = DateTimeOffset.UtcNow;

		return Result.Result.Success();
	}

	public Result.Result Fail(Guid attemptId)
	{
		if (!IsCurrentAttempt(attemptId))
			return MapProcessingErrors.StaleAttempt;

		Status = MapProcessingStatus.Failed;
		ErrorCode = MapProcessingErrors.ProcessingFailed.Code;
		ErrorMessage = MapProcessingErrors.ProcessingFailed.Message;
		UpdatedAt = DateTimeOffset.UtcNow;

		return Result.Result.Success();
	}

	public Result.Result Retry()
	{
		if (Status != MapProcessingStatus.Failed)
			return MapProcessingErrors.RetryRequiresFailure;

		Reset();

		return Result.Result.Success();
	}

	public bool Recover()
	{
		if (Status != MapProcessingStatus.Processing)
			return false;

		Reset();

		return true;
	}

	private bool IsCurrentAttempt(Guid attemptId) =>
		Status == MapProcessingStatus.Processing && AttemptId == attemptId;

	private void Reset()
	{
		Status = MapProcessingStatus.Queued;
		Progress = 0;
		ErrorCode = null;
		ErrorMessage = null;
		AttemptId = Guid.CreateVersion7();
		UpdatedAt = DateTimeOffset.UtcNow;
	}

	private static string NormalizeColor(string color)
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
