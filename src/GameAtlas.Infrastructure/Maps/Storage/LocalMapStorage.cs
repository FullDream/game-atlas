using GameAtlas.Application.Maps.Storage;
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

	public string WorkPath(Guid mapId, Guid styleId, Guid attemptId) =>
		Resolve("work", Key(mapId), Key(styleId), Key(attemptId));

	public string TilesPath(Guid mapId, Guid styleId) => Resolve("tiles", Key(mapId), Key(styleId));

	public void Publish(Guid mapId, Guid styleId, Guid attemptId)
	{
		var work = WorkPath(mapId, styleId, attemptId);
		var destination = TilesPath(mapId, styleId);

		if (!File.Exists(Path.Combine(work, "manifest.json")))
			throw new InvalidDataException("The tile manifest is missing.");

		Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
		Directory.Move(work, destination);
	}


	public void DeleteOwned(Guid mapId, Guid? styleId = null)
	{
		var keys = new List<string> { Key(mapId) };

		if (styleId.HasValue)
			keys.Add(Key(styleId.Value));

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
