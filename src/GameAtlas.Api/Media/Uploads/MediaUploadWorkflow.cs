using System.Text;
using GameAtlas.Api.Http;
using GameAtlas.Api.Results;
using GameAtlas.Application.Media;
using GameAtlas.Application.Media.Assets.AdmitMediaAsset;
using GameAtlas.Application.Media.Inspection;
using GameAtlas.Application.Media.Storage;
using GameAtlas.Domain;
using Mediator;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace GameAtlas.Api.Media.Uploads;

internal sealed class MediaUploadWorkflow(IMediaStorage storage,
	IMediaInspector inspector, IOptions<MediaLimits> options, IMediator mediator)
{
	public async Task<IResult> UploadAsync(HttpRequest request, CancellationToken cancellationToken)
	{
		if (!MediaTypeHeaderValue.TryParse(request.ContentType, out var contentType) ||
			!string.Equals(contentType.MediaType.Value, "multipart/form-data", StringComparison.OrdinalIgnoreCase))
			return Failure(415);

		var boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary).Value;

		if (string.IsNullOrEmpty(boundary) || boundary.Length > 128)
			return Failure(400);

		var assetId = Guid.CreateVersion7();
		var uploadId = Guid.CreateVersion7();

		try
		{
			return await ReadAsync(request, boundary, uploadId, assetId, cancellationToken);
		}
		finally
		{
			storage.DeleteStaging(uploadId);
		}
	}

	private async Task<IResult> ReadAsync(HttpRequest request, string boundary, Guid uploadId, Guid assetId,
		CancellationToken cancellationToken)
	{
		var bodyLimit = checked(options.Value.MaximumBytes + 65536);
		var bodyFeature = request.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
		var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		var reader = new MultipartReader(boundary, new BoundedReadStream(request.Body, bodyLimit));
		string? filename = null;
		long? byteLength = null;

		if (bodyFeature is { IsReadOnly: false })
			bodyFeature.MaxRequestBodySize = bodyLimit;

		try
		{
			while (await reader.ReadNextSectionAsync(cancellationToken) is { } section)
			{
				if (!ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var disposition) ||
					!string.Equals(disposition.DispositionType.Value, "form-data", StringComparison.OrdinalIgnoreCase))
					return Failure(400);

				var key = HeaderUtilities.RemoveQuotes(disposition.Name).Value;

				if (key is null)
					return Failure(400);

				if (key.Equals("file", StringComparison.OrdinalIgnoreCase))
				{
					if (byteLength.HasValue || !disposition.FileName.HasValue && !disposition.FileNameStar.HasValue)
						return Failure(400);

					filename = HeaderUtilities.RemoveQuotes(disposition.FileNameStar.HasValue
						? disposition.FileNameStar : disposition.FileName).Value?.Replace('\\', '/').Split('/').Last();
					var result = await storage.WriteAsync(uploadId, section.Body, options.Value.MaximumBytes,
						cancellationToken);

					if (result.IsFailure)
						return Failure(413);

					byteLength = result.Value;
				}
				else
				{
					if (!new[] { "name", "folderId", "alt", "previewAssetId" }.Contains(key,
						StringComparer.OrdinalIgnoreCase) || fields.ContainsKey(key) || disposition.FileName.HasValue)
						return Failure(400);

					using var buffer = new MemoryStream();
					var text = new BoundedReadStream(section.Body, 8192);

					await text.CopyToAsync(buffer, cancellationToken);
					fields.Add(key, new UTF8Encoding(false, true).GetString(buffer.ToArray()));
				}
			}
		}
		catch (BadHttpRequestException exception) when (exception.StatusCode is 400 or 413)
		{
			return Failure(exception.StatusCode);
		}
		catch (Exception exception) when (exception is InvalidDataException or DecoderFallbackException)
		{
			return Failure(400);
		}

		if (byteLength is null)
			return Failure(400);

		Guid? folderId = null;
		Optional<Guid?> previewId = default;

		if (fields.TryGetValue("folderId", out var folder))
		{
			if (!Guid.TryParse(folder, out var id))
				return Failure(400);

			folderId = id;
		}

		if (fields.TryGetValue("previewAssetId", out var preview))
		{
			if (!Guid.TryParse(preview, out var id))
				return Failure(400);

			previewId = id;
		}

		var inspection = await inspector.InspectAsync(storage.StagingPath(uploadId), cancellationToken);

		if (inspection.IsFailure)
			return new OutcomeResult(inspection.Errors);

		var alt = fields.TryGetValue("alt", out var altText) ? Optional<string?>.From(altText) : default;
		var name = fields.TryGetValue("name", out var suppliedName) ? suppliedName : filename;
		var admitted = await mediator.Send(new AdmitMediaAsset(uploadId, assetId,
			name, folderId, alt, previewId, byteLength.Value, inspection.Value), cancellationToken);

		return new OutcomeResult<MediaAssetDto>(admitted);
	}

	private static IResult Failure(int statusCode) => TypedResults.Problem(statusCode: statusCode);
}
