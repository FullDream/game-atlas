using GameAtlas.Api.OpenApi.Results;
using Microsoft.AspNetCore.Mvc;

namespace GameAtlas.Api.Results;

public static class OutcomeResultExtensions
{
	/// <summary>
	/// Adds OutcomeResult metadata integration to MVC explicitly registered by the host.
	/// </summary>
	public static IMvcBuilder AddOutcomeResults(this IMvcBuilder builder)
	{
		builder.Services.Configure<MvcOptions>(options =>
		{
			if (!options.Conventions.OfType<OutcomeResultProducesResponseConvention>().Any())
				options.Conventions.Add(new OutcomeResultProducesResponseConvention());
		});

		return builder;
	}

	/// <summary>
	/// Registers automatic OutcomeResult response metadata on an API group.
	/// Applies to all nested groups and endpoints after their explicit conventions.
	/// </summary>
	public static RouteGroupBuilder WithOutcomeResults(this RouteGroupBuilder group)
	{
		((IEndpointConventionBuilder)group).Finally(OutcomeResultMetadataConvention.Apply);

		return group;
	}
}
