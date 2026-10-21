namespace GameAtlas.Api.OpenApi.Validation;

internal sealed class OpenApiValidationSourceResolver(IEnumerable<IOpenApiValidationSource> sources)
	: IOpenApiValidationSourceResolver
{
	private readonly IReadOnlyDictionary<Type, Type> mappings = sources.ToDictionary(
		x => x.ContractType,
		x => x.ValidationType);

	public Type Resolve(Type contractType) => mappings.GetValueOrDefault(contractType, contractType);
}
