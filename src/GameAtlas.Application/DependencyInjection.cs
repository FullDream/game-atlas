using FluentValidation;
using GameAtlas.Application.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.Application;

public static class DependencyInjection
{
	public static IServiceCollection AddApplication(this IServiceCollection services)
	{
		services.AddMediator(options =>
		{
			options.ServiceLifetime = ServiceLifetime.Scoped;
			options.PipelineBehaviors = [typeof(ValidationBehavior<,>)];
		});

		services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

		return services;
	}
}
