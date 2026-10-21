namespace GameAtlas.Api.OpenApi.Validation;

internal sealed class OpenApiValidationSource<TContract, TValidation> : IOpenApiValidationSource
{
	public Type ContractType => typeof(TContract);

	public Type ValidationType => typeof(TValidation);
}
