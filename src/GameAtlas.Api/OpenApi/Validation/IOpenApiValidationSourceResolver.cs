namespace GameAtlas.Api.OpenApi.Validation;

public interface IOpenApiValidationSourceResolver
{
	Type Resolve(Type contractType);
}
