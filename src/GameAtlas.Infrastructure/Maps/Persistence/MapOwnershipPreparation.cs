using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameAtlas.Domain.Maps;
using GameAtlas.Domain.Result;
using Microsoft.Data.Sqlite;

namespace GameAtlas.Infrastructure.Maps.Persistence;

public static class MapOwnershipPreparation
{
	public static async Task<Result> PrepareFileAsync(string databasePath, string inputPath,
		CancellationToken cancellationToken = default)
	{
		MapOwnershipInput? input;

		try
		{
			await using var file = File.OpenRead(inputPath);
			input = await JsonSerializer.DeserializeAsync<MapOwnershipInput>(file, new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
			}, cancellationToken);
		}
		catch (JsonException)
		{
			return MapOwnershipErrors.Invalid("The mapping must be a valid JSON object with maps and groups arrays.");
		}

		return input is null ? MapOwnershipErrors.Invalid("The mapping must not be null.")
			: await PrepareAsync(databasePath, input, cancellationToken);
	}

	public static async Task<Result> PrepareAsync(string databasePath, MapOwnershipInput input,
		CancellationToken cancellationToken = default)
	{
		if (input.Maps is null || input.Groups is null)
			return MapOwnershipErrors.Invalid("Both maps and groups arrays are required.");

		var connectionString = new SqliteConnectionStringBuilder
		{
			DataSource = Path.GetFullPath(databasePath),
			Mode = SqliteOpenMode.ReadWrite,
			Pooling = false,
			ForeignKeys = true
		}.ToString();
		await using var connection = new SqliteConnection(connectionString);

		await connection.OpenAsync(cancellationToken);
		using var transaction = connection.BeginTransaction();
		var history = await ReadAsync(connection, transaction,
			"SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC LIMIT 1", cancellationToken);

		if (history.Count != 1 || (string)history[0][0] != "20261023210838_SimplifyMapStyles")
			return MapOwnershipErrors.Invalid(
				"Prepare a database at SimplifyMapStyles before applying MapOwnedContent.");

		var gameRows = await ReadAsync(connection, transaction, "SELECT Id FROM Games", cancellationToken);
		var games = gameRows.Select(row => Guid.Parse((string)row[0])).ToHashSet();
		var mapRows = await ReadAsync(connection, transaction,
			"SELECT Id, GameId, Width, Height FROM Maps", cancellationToken);

		var groups = await ReadAsync(connection, transaction, "SELECT Id, GameId FROM CategoryGroups",
			cancellationToken);

		var prepared = new Dictionary<Guid, (MapOwnershipMap Input, Map Map)>();
		var names = new HashSet<(Guid GameId, string Name)>();

		foreach (var item in input.Maps)
		{
			if (item is null || item.Id == Guid.Empty || !games.Contains(item.GameId) || prepared.ContainsKey(item.Id))
				return MapOwnershipErrors.Invalid("Every map needs a distinct nonempty ID and an existing game.");

			var map = Map.Create(item.GameId, item.Name, item.Width, item.Height);

			if (map.IsFailure)
				return map.Errors;

			if (!names.Add((item.GameId, map.Value.NormalizedName)))
				return MapOwnershipErrors.Invalid("Prepared map names must be unique within each game.");

			prepared.Add(item.Id, (item, map.Value));
		}

		foreach (var row in mapRows)
		{
			var id = Guid.Parse((string)row[0]);

			if (!prepared.TryGetValue(id, out var item) || item.Input.GameId != Guid.Parse((string)row[1]) ||
				item.Input.Width != Convert.ToInt32(row[2]) || item.Input.Height != Convert.ToInt32(row[3]))
				return MapOwnershipErrors.Invalid("Every existing map must retain its ID, game, and exact dimensions.");
		}

		var assignments = new Dictionary<Guid, Guid>();

		foreach (var item in input.Groups)
		{
			if (item is null || !assignments.TryAdd(item.Id, item.MapId))
				return MapOwnershipErrors.Invalid("Every group must have exactly one destination.");
		}

		if (assignments.Count != groups.Count)
			return MapOwnershipErrors.Invalid("Supply exactly one assignment for every existing group.");

		foreach (var row in groups)
		{
			if (!assignments.TryGetValue(Guid.Parse((string)row[0]), out var destination) ||
				!prepared.TryGetValue(destination, out var map) || map.Input.GameId != Guid.Parse((string)row[1]))
				return MapOwnershipErrors.Invalid("A group must be assigned to a prepared map of its existing game.");
		}

		await ExecuteAsync(connection, transaction, """
			CREATE TABLE IF NOT EXISTS __MapOwnershipMaps (
				Id TEXT PRIMARY KEY, GameId TEXT NOT NULL, Name TEXT NOT NULL, NormalizedName TEXT NOT NULL,
				Width INTEGER NOT NULL, Height INTEGER NOT NULL, CreatedAt TEXT NOT NULL);
			CREATE TABLE IF NOT EXISTS __MapOwnershipGroups (Id TEXT PRIMARY KEY, MapId TEXT NOT NULL);
			DELETE FROM __MapOwnershipGroups;
			DELETE FROM __MapOwnershipMaps;
			""", cancellationToken);

		foreach (var (id, item) in prepared)
		{
			await ExecuteAsync(connection, transaction, """
				INSERT INTO __MapOwnershipMaps VALUES ($id, $game, $name, $normalized, $width, $height, $created)
				""", cancellationToken, ("$id", id), ("$game", item.Input.GameId), ("$name", item.Map.Name),
				("$normalized", item.Map.NormalizedName), ("$width", item.Map.Width), ("$height", item.Map.Height),
				("$created", item.Map.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss.FFFFFFFzzz",
					CultureInfo.InvariantCulture)));
		}

		foreach (var (id, mapId) in assignments)
			await ExecuteAsync(connection, transaction, "INSERT INTO __MapOwnershipGroups VALUES ($id, $map)",
				cancellationToken, ("$id", id), ("$map", mapId));

		await transaction.CommitAsync(cancellationToken);

		return Result.Success();
	}

	private static async Task<List<object[]>> ReadAsync(SqliteConnection connection, SqliteTransaction transaction,
		string sql, CancellationToken cancellationToken)
	{
		await using var command = connection.CreateCommand();

		command.Transaction = transaction;
		command.CommandText = sql;
		await using var reader = await command.ExecuteReaderAsync(cancellationToken);
		var rows = new List<object[]>();

		while (await reader.ReadAsync(cancellationToken))
		{
			var row = new object[reader.FieldCount];

			reader.GetValues(row);
			rows.Add(row);
		}

		return rows;
	}

	private static async Task ExecuteAsync(SqliteConnection connection, SqliteTransaction transaction, string sql,
		CancellationToken cancellationToken, params (string Name, object Value)[] parameters)
	{
		await using var command = connection.CreateCommand();

		command.Transaction = transaction;
		command.CommandText = sql;

		foreach (var (name, value) in parameters)
			command.Parameters.AddWithValue(name, value);

		await command.ExecuteNonQueryAsync(cancellationToken);
	}
}
