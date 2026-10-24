using GameAtlas.Application.Maps.Storage;
using GameAtlas.Infrastructure.Maps.Storage;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace GameAtlas.IntegrationTests;

public sealed class MapStorageTests : IDisposable
{
	private readonly string root = Path.Combine(Path.GetTempPath(), $"map-storage-{Guid.NewGuid():N}");
	private readonly LocalMapStorage storage;

	public MapStorageTests() => storage = new LocalMapStorage(
		Options.Create(new MapStorageOptions { StorageRoot = "maps" }), new Environment(root));

	[Fact]
	public async Task StreamFlushPromotionAndOwnedDeletionPreserveOriginalBytes()
	{
		var session = Guid.NewGuid();
		var file = Guid.NewGuid();
		var map = Guid.NewGuid();
		var style = Guid.NewGuid();
		var bytes = Enumerable.Range(0, 65537).Select(index => (byte)index).ToArray();
		var renewals = 0;

		await storage.WriteAsync(session, file, new MemoryStream(bytes), bytes.Length,
			_ => { renewals++; return Task.CompletedTask; }, CancellationToken.None);
		storage.Promote(session, file, map, style);
		Assert.Equal(bytes, await File.ReadAllBytesAsync(storage.OriginalPath(map, style)));
		Assert.True(renewals >= 3);
		Assert.False(File.Exists(storage.StagingPath(session, file)));
		var unrelatedMap = Guid.NewGuid();
		var unrelatedPath = storage.OriginalPath(unrelatedMap, style);

		Directory.CreateDirectory(Path.GetDirectoryName(unrelatedPath)!);
		await File.WriteAllTextAsync(unrelatedPath, "unrelated");
		storage.DeleteOwned(map);
		storage.DeleteOwned(map);
		storage.DeleteStaging(session);
		Assert.True(File.Exists(unrelatedPath));
		Assert.False(File.Exists(storage.OriginalPath(map, style)));
	}

	[Fact]
	public async Task LimitsCancellationAndExpiredLeasePreventFurtherWrites()
	{
		var session = Guid.NewGuid();
		var file = Guid.NewGuid();

		var oversized = await storage.WriteAsync(session, file,
			new MemoryStream(new byte[101]), 100, _ => Task.CompletedTask, CancellationToken.None);

		Assert.True(oversized.IsFailure);
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => storage.WriteAsync(session, Guid.NewGuid(),
			new MemoryStream(new byte[1]), 100, _ => Task.CompletedTask, new CancellationToken(true)));
		await Assert.ThrowsAsync<InvalidOperationException>(() => storage.WriteAsync(session, Guid.NewGuid(),
			new MemoryStream(new byte[1]), 100, _ => throw new InvalidOperationException("Expired"),
			CancellationToken.None));
	}

	[Fact]
	public void GeneratedPathsAndOptionsRejectInvalidOwnersAndLimits()
	{
		Assert.Throws<ArgumentException>(() => storage.StagingPath(Guid.Empty, Guid.NewGuid()));
		Assert.Throws<ArgumentException>(() => storage.TilesPath(Guid.Empty, Guid.NewGuid()));
		var validator = new MapStorageOptionsValidation();

		Assert.True(validator.Validate(null, new MapUploadLimits()).Succeeded);
		Assert.True(validator.Validate(null, new MapUploadLimits { FileBytes = 0 }).Failed);
		Assert.True(validator.Validate(null, new MapUploadLimits { MaximumDimension = 16385 }).Failed);
		Assert.True(validator.Validate(null, new MapStorageOptions { StorageRoot = "" }).Failed);
	}

	[Fact]
	public void JunctionEscapeIsRejectedBeforeAnyReadOrDeletion()
	{
		var map = Guid.NewGuid();
		var outside = Path.Combine(root, "outside");
		var link = Path.Combine(root, "maps", "content", map.ToString("N"));

		Directory.CreateDirectory(outside);
		Directory.CreateDirectory(Path.GetDirectoryName(link)!);
		File.WriteAllText(Path.Combine(outside, "keep"), "protected");
		if (!OperatingSystem.IsWindows())
		{
			Directory.CreateSymbolicLink(link, outside);
		}
		else
		{
			var start = new System.Diagnostics.ProcessStartInfo("cmd.exe")
			{
				CreateNoWindow = true,
				UseShellExecute = false
			};

			start.ArgumentList.Add("/c");
			start.ArgumentList.Add("mklink");
			start.ArgumentList.Add("/J");
			start.ArgumentList.Add(link);
			start.ArgumentList.Add(outside);
			using var process = System.Diagnostics.Process.Start(start)!;

			process.WaitForExit();
			Assert.Equal(0, process.ExitCode);
		}

		try
		{
			Assert.Throws<IOException>(() => storage.DeleteOwned(map));
			Assert.True(File.Exists(Path.Combine(outside, "keep")));
		}
		finally
		{
			Directory.Delete(link);
		}
	}

	public void Dispose() => Directory.Delete(root, true);

	private sealed class Environment(string contentRoot) : IHostEnvironment
	{
		public string EnvironmentName { get; set; } = "Testing";
		public string ApplicationName { get; set; } = "GameAtlas";
		public string ContentRootPath { get; set; } = contentRoot;
		public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
	}
}
