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
	public void GeneratedPathsAndOptionsRejectInvalidOwnersAndLimits()
	{
		Assert.Throws<ArgumentException>(() => storage.WorkPath(Guid.Empty, Guid.NewGuid(), Guid.NewGuid()));
		Assert.Throws<ArgumentException>(() => storage.TilesPath(Guid.Empty, Guid.NewGuid()));
		var validator = new MapStorageOptionsValidation();

		Assert.True(validator.Validate(null, new MapStorageOptions()).Succeeded);
		Assert.True(validator.Validate(null, new MapStorageOptions { StorageRoot = "" }).Failed);
	}

	[Fact]
	public void JunctionEscapeIsRejectedBeforeAnyReadOrDeletion()
	{
		var map = Guid.NewGuid();
		var outside = Path.Combine(root, "outside");
		var link = Path.Combine(root, "maps", "work", map.ToString("N"));

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
