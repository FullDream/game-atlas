using System.Text;
using GameAtlas.Api.Results;
using GameAtlas.Application.Maps.Images;
using GameAtlas.Application.Maps.Storage;
using GameAtlas.Application.Maps.Uploads;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace GameAtlas.Api.Maps.Uploads;

internal sealed class MapStyleMultipartReader(IMapStorage storage, IMapAdmissionSessions sessions,
	IMapImageProcessor images, IOptions<MapUploadLimits> options)
{
	public async Task<MapStyleUploadReadResult> ReadAsync(HttpRequest request, Guid sessionId,
		CancellationToken cancellationToken)
	{
		if (!MediaTypeHeaderValue.TryParse(request.ContentType, out var contentType) ||
			!string.Equals(contentType.MediaType.Value, "multipart/form-data", StringComparison.OrdinalIgnoreCase))
			return Failure(StatusCodes.Status415UnsupportedMediaType);

		var boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary).Value;

		if (string.IsNullOrEmpty(boundary) || boundary.Length > 128)
			return Failure(StatusCodes.Status400BadRequest);

		var limits = options.Value;
		var bodyLimit = limits.FileBytes + limits.EnvelopeBytes;
		var bodyFeature = request.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
		var reader = new MultipartReader(boundary, new BoundedReadStream(request.Body, bodyLimit));
		var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		Guid? fileId = null;

		if (bodyFeature is { IsReadOnly: false })
			bodyFeature.MaxRequestBodySize = bodyLimit;

		try
		{
			while (await reader.ReadNextSectionAsync(cancellationToken) is { } section)
			{
				if (!ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var disposition) ||
					!string.Equals(disposition.DispositionType.Value, "form-data", StringComparison.OrdinalIgnoreCase))
					return Failure(StatusCodes.Status400BadRequest);

				var key = HeaderUtilities.RemoveQuotes(disposition.Name).Value;

				if (key is null)
					return Failure(StatusCodes.Status400BadRequest);

				if (string.Equals(key, nameof(MapUploadRequest.File), StringComparison.OrdinalIgnoreCase))
				{
					if (fileId.HasValue)
						return Failure(StatusCodes.Status400BadRequest);

					fileId = Guid.CreateVersion7();
					var result = await storage.WriteAsync(sessionId, fileId.Value, section.Body, limits.FileBytes,
						token => sessions.RenewAsync(sessionId, token), cancellationToken);

					if (result.IsFailure)
						return Failure(StatusCodes.Status413PayloadTooLarge);
				}
				else if (string.Equals(key, nameof(MapUploadRequest.Name), StringComparison.OrdinalIgnoreCase) ||
					string.Equals(key, nameof(MapUploadRequest.BackgroundColor), StringComparison.OrdinalIgnoreCase))
				{
					if (fields.ContainsKey(key))
						return Failure(StatusCodes.Status400BadRequest);

					using var buffer = new MemoryStream();
					var text = new BoundedReadStream(section.Body, limits.MetadataBytes);

					await text.CopyToAsync(buffer, cancellationToken);
					fields.Add(key, new UTF8Encoding(false, true).GetString(buffer.ToArray()));
				}
				else
				{
					return Failure(StatusCodes.Status400BadRequest);
				}
			}
		}
		catch (BadHttpRequestException exception) when (exception.StatusCode is 400 or 413)
		{
			return Failure(exception.StatusCode);
		}
		catch (Exception exception) when (exception is InvalidDataException or DecoderFallbackException)
		{
			return Failure(StatusCodes.Status400BadRequest);
		}

		if (fileId is not { } id)
			return Failure(StatusCodes.Status400BadRequest);

		var image = images.Probe(storage.StagingPath(sessionId, id));

		if (image.IsFailure)
			return new MapStyleUploadReadResult(null, new OutcomeResult(image.Errors));

		if (image.Value.Width > limits.MaximumDimension || image.Value.Height > limits.MaximumDimension)
			return new MapStyleUploadReadResult(null,
				new OutcomeResult(MapUploadErrors.InvalidDimensions(limits.MaximumDimension)));

		var metadata = new MapUploadMetadata(fields.GetValueOrDefault(nameof(MapUploadRequest.Name)),
			fields.GetValueOrDefault(nameof(MapUploadRequest.BackgroundColor)));

		return new MapStyleUploadReadResult(new StagedMapUpload(id, metadata, image.Value));
	}

	private static MapStyleUploadReadResult Failure(int statusCode) =>
		new(null, TypedResults.Problem(statusCode: statusCode));
}
