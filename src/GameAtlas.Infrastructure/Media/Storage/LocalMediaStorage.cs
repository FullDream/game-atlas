using GameAtlas.Application.Media.Storage;
using GameAtlas.Domain.Result;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace GameAtlas.Infrastructure.Media.Storage;

public sealed class LocalMediaStorage : IMediaStorage
{
	private readonly string root;

	public LocalMediaStorage(IOptions<MediaStorageOptions> options, IHostEnvironment? environment = null)
	{
		root = Path.GetFullPath(options.Value.StorageRoot, environment?.ContentRootPath ?? AppContext.BaseDirectory);

		RejectLinks(root);
		Directory.CreateDirectory(PublicAssetsRoot);
	}

	public string PublicAssetsRoot => Path.Combine(root, "assets");

	public string AssetPath(Guid assetId) => Resolve("assets", assetId);

	public string StagingPath(Guid uploadId) => Resolve("staging", uploadId);

	public async Task<Result<long>> WriteAsync(Guid uploadId, Stream source, long maximumBytes,
		CancellationToken cancellationToken)
	{
		var path = StagingPath(uploadId);

		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		await using var destination = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None,
			65536, FileOptions.Asynchronous | FileOptions.WriteThrough);
		var buffer = new byte[65536];
		long total = 0;

		while (true)
		{
			var count = await source.ReadAsync(buffer, cancellationToken);

			if (count == 0)
				break;

			if (count > maximumBytes - total)
				return MediaStorageErrors.ByteLimit;

			total += count;
			await destination.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
		}

		await destination.FlushAsync(cancellationToken);
		destination.Flush(flushToDisk: true);

		return total;
	}

	public void Promote(Guid uploadId, Guid assetId)
	{
		var destination = AssetPath(assetId);

		Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
		File.Move(StagingPath(uploadId), destination);
	}

	public void DeleteStaging(Guid uploadId) => DeleteFile(StagingPath(uploadId));

	public void DeleteAsset(Guid assetId) => DeleteFile(AssetPath(assetId));

	private static void DeleteFile(string path)
	{
		try
		{
			File.Delete(path);
		}
		catch (DirectoryNotFoundException)
		{
			// A missing generated directory also means there is no owned file left to remove.
		}
	}

	private string Resolve(string area, Guid id)
	{
		if (id == Guid.Empty)
			throw new ArgumentException("A generated nonempty identifier is required.", nameof(id));

		var path = Path.Combine(root, area, id.ToString("N"));

		RejectLinks(path);

		return path;
	}

	private static void RejectLinks(string path)
	{
		for (var entry = new FileInfo(path) as FileSystemInfo; entry is not null;
			entry = entry is DirectoryInfo directory ? directory.Parent : ((FileInfo)entry).Directory)
		{
			if (entry.LinkTarget is not null || entry.Exists && entry.Attributes.HasFlag(FileAttributes.ReparsePoint))
				throw new IOException("Media storage cannot contain symbolic links or reparse points.");
		}
	}
}
