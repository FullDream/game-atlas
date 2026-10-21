using System.Text.Json;

namespace GameAtlas.Api.Serialization;

public class SegmentedNamingPolicy(JsonNamingPolicy namingPolicy, char separator = '.') : JsonNamingPolicy
{
	public override string ConvertName(string name) =>
		name.All(c => c != separator)
			? namingPolicy.ConvertName(name)
			: string.Join(separator, name.Split(separator).Select(namingPolicy.ConvertName));
}