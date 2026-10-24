using GameAtlas.Application.Maps.Storage;
using GameAtlas.Application.Maps.Uploads;
using GameAtlas.Domain.Result;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace GameAtlas.Infrastructure.Maps.Storage;

public sealed class LocalMapStorage : IMapStorage
{
	private readonly string root;

	public LocalMapStorage(IOptions<MapStorageOptions> options, IHostEnvironment? environment = null)
	{
		root = Path.GetFullPath(options.Value.StorageRoot, environment?.ContentRootPath ?? AppContext.BaseDirectory);
		RejectLinks(root);
		Directory.CreateDirectory(root);
		PublicTilesRoot = Resolve("tiles");
		Directory.CreateDirectory(PublicTilesRoot);
	}

	public string PublicTilesRoot { get; }

	public string StagingPath(Guid sessionId, Guid fileId) => Resolve("staging", Key(sessionId), Key(fileId));

	public string OriginalPath(Guid mapId, Guid styleId) =>
		Resolve("content", Key(mapId), Key(styleId), "original");

	public string WorkPath(Guid mapId, Guid styleId, Guid attemptId) =>
		Resolve("work", Key(mapId), Key(styleId), Key(attemptId));

	public string TilesPath(Guid mapId, Guid styleId) => Resolve("tiles", Key(mapId), Key(styleId));

	public async Task<Result<long>> WriteAsync(Guid sessionId, Guid fileId, Stream source, long maximumBytes,
		Func<CancellationToken, Task> renewLease, CancellationToken cancellationToken)
	{
		var path = StagingPath(sessionId, fileId);

		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		await renewLease(cancellationToken);
		await using var destination = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None,
			64 * 1024, FileOptions.Asynchronous | FileOptions.WriteThrough);

		var buffer = new byte[64 * 1024];
		long total = 0;

		while (true)
		{
			var count = await source.ReadAsync(buffer, cancellationToken);

			if (count == 0)
				break;

			total += count;

			if (total > maximumBytes)
				return MapUploadErrors.ByteLimit;

			await renewLease(cancellationToken);
			await destination.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
		}

		await destination.FlushAsync(cancellationToken);
		destination.Flush(flushToDisk: true);

		return total;
	}

	public void Promote(Guid sessionId, Guid fileId, Guid mapId, Guid styleId)
	{
		var destination = OriginalPath(mapId, styleId);

		Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
		File.Move(StagingPath(sessionId, fileId), destination);
	}

	public void Publish(Guid mapId, Guid styleId, Guid attemptId)
	{
		var work = WorkPath(mapId, styleId, attemptId);
		var destination = TilesPath(mapId, styleId);

		if (!File.Exists(Path.Combine(work, "manifest.json")))
			throw new InvalidDataException("The tile manifest is missing.");

		Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
		Directory.Move(work, destination);
	}

	public void DeleteStaging(Guid sessionId) => DeleteDirectory(Resolve("staging", Key(sessionId)));

	public void DeleteOwned(Guid mapId, Guid? styleId = null)
	{
		var keys = new List<string> { Key(mapId) };

		if (styleId.HasValue)
			keys.Add(Key(styleId.Value));

		DeleteDirectory(Resolve(["content", .. keys]));
		DeleteDirectory(Resolve(["work", .. keys]));
		DeleteDirectory(Resolve(["tiles", .. keys]));
	}

	public void DeleteWork(Guid mapId, Guid styleId) => DeleteDirectory(Resolve("work", Key(mapId), Key(styleId)));

	public void DeleteTiles(Guid mapId, Guid styleId) => DeleteDirectory(TilesPath(mapId, styleId));

	private string Resolve(params string[] segments)
	{
		var path = Path.GetFullPath(Path.Combine([root, .. segments]));

		if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
			throw new IOException("Map storage paths must remain inside the configured root.");

		RejectLinks(path);

		return path;
	}

	private static string Key(Guid id)
	{
		if (id == Guid.Empty)
			throw new ArgumentException("Storage identifiers must not be empty.", nameof(id));

		return id.ToString("N");
	}

	private static void RejectLinks(string path)
	{
		for (var current = new FileInfo(path) as FileSystemInfo; current is not null;
			 current = Directory.GetParent(current.FullName))
		{
			if ((current.Exists || current.LinkTarget is not null) &&
				(current.Attributes & FileAttributes.ReparsePoint) != 0)
				throw new IOException("Map storage must not traverse symbolic links or junctions.");
		}
	}

	private static void DeleteDirectory(string path)
	{
		if (!Directory.Exists(path))
			return;

		// Inspect every descendant before recursive deletion, including directory links.
		foreach (var entry in new DirectoryInfo(path).EnumerateFileSystemInfos())
		{
			RejectLinks(entry.FullName);

			if (entry is DirectoryInfo)
				DeleteDirectory(entry.FullName);
			else
				entry.Delete();
		}

		Directory.Delete(path);
	}
}
