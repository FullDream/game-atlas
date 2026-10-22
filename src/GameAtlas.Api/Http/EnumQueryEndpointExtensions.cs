using System.Reflection;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.Primitives;

namespace GameAtlas.Api.Http;

internal static class EnumQueryEndpointExtensions
{
	internal static RouteGroupBuilder WithCaseInsensitiveEnumQueryParameters(this RouteGroupBuilder group)
	{
		IEndpointConventionBuilder conventions = group;

		conventions.Finally(ApplyEnumQueryBinding);

		return group;
	}

	private static void ApplyEnumQueryBinding(EndpointBuilder endpoint)
	{
		var parameters = new List<(string Name, Type Type)>();

		foreach (var parameter in endpoint.Metadata.OfType<IParameterBindingMetadata>())
		{
			var type = Nullable.GetUnderlyingType(parameter.ParameterInfo.ParameterType) ??
				parameter.ParameterInfo.ParameterType;
			var query = parameter.ParameterInfo.GetCustomAttributes().OfType<IFromQueryMetadata>().FirstOrDefault();

			if (type.IsEnum && query is not null)
				parameters.Add((query.Name ?? parameter.Name, type));
		}

		if (parameters.Count == 0)
			return;

		var next = endpoint.RequestDelegate ??
			throw new InvalidOperationException("The endpoint request delegate must be initialized.");

		endpoint.RequestDelegate = context =>
		{
			Dictionary<string, StringValues>? normalized = null;

			foreach (var parameter in parameters)
			{
				if (!context.Request.Query.TryGetValue(parameter.Name, out var values))
					continue;

				var value = values.ToString();

				if (!Enum.TryParse(parameter.Type, value, ignoreCase: true, out var parsed) || parsed.ToString() == value)
					continue;

				normalized ??= new Dictionary<string, StringValues>(context.Request.Query, StringComparer.OrdinalIgnoreCase);
				normalized[parameter.Name] = parsed.ToString();
			}

			if (normalized is not null)
				context.Request.Query = new QueryCollection(normalized);

			return next(context);
		};
	}
}
