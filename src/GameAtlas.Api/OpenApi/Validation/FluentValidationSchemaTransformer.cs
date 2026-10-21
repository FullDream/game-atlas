using System.Globalization;
using System.Reflection;
using FluentValidation;
using FluentValidation.Validators;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace GameAtlas.Api.OpenApi.Validation;

public sealed class FluentValidationSchemaTransformer(IOpenApiValidationSourceResolver validationSourceResolver)
	: IOpenApiSchemaTransformer
{
	public Task TransformAsync(
		OpenApiSchema schema,
		OpenApiSchemaTransformerContext context,
		CancellationToken cancellationToken)
	{
		if (schema.Type?.HasFlag(JsonSchemaType.Object) != true || schema.Properties is not { Count: > 0 })
		{
			return Task.CompletedTask;
		}

		var validationType = validationSourceResolver.Resolve(context.JsonTypeInfo.Type);

		var validatorType = typeof(IValidator<>).MakeGenericType(validationType);

		if (context.ApplicationServices.GetService(validatorType) is not IValidator validator)
		{
			return Task.CompletedTask;
		}

		var descriptor = validator.CreateDescriptor();

		foreach (var jsonProperty in context.JsonTypeInfo.Properties)
		{
			if (jsonProperty.AttributeProvider is not PropertyInfo propertyInfo)
				continue;

			if (!schema.Properties.TryGetValue(jsonProperty.Name, out var propertySchema))
			{
				continue;
			}

			if (propertySchema is not OpenApiSchema mutableSchema)
				continue;

			var rules = descriptor.GetRulesForMember(propertyInfo.Name);

			foreach (var rule in rules)
			{
				foreach (var component in rule.Components)
				{
					ApplyValidator(component.Validator, mutableSchema);
				}
			}
		}

		return Task.CompletedTask;
	}

	private static void ApplyValidator(IPropertyValidator validator, OpenApiSchema schema)
	{
		switch (validator)
		{
			case INotNullValidator:
				RemoveNull(schema);
				break;

			case INotEmptyValidator:
				ApplyNotEmpty(schema);
				break;

			case IMinimumLengthValidator minimumLength:
				ApplyMinLength(schema, minimumLength.Min);
				break;

			case IMaximumLengthValidator maximumLength:
				ApplyMaxLength(schema, maximumLength.Max);
				break;

			case ILengthValidator length:
				ApplyMinLength(schema, length.Min);
				ApplyMaxLength(schema, length.Max);
				break;

			case IRegularExpressionValidator regex:
				schema.Pattern = regex.Expression;
				break;
		}

		ApplyKnownValidator(validator, schema);
	}

	private static void ApplyNotEmpty(OpenApiSchema schema)
	{
		RemoveNull(schema);

		if (schema.Type?.HasFlag(JsonSchemaType.String) == true)
		{
			schema.MinLength = Math.Max(schema.MinLength ?? 0, 1);
		}

		if (schema.Type?.HasFlag(JsonSchemaType.Array) == true)
		{
			schema.MinItems = Math.Max(schema.MinItems ?? 0, 1);
		}
	}

	private static void ApplyMinLength(OpenApiSchema schema, int value)
	{
		if (value <= 0)
			return;

		schema.MinLength = Math.Max(schema.MinLength ?? 0, value);
	}

	private static void ApplyMaxLength(OpenApiSchema schema, int value)
	{
		if (value < 0)
			return;

		schema.MaxLength = schema.MaxLength is { } current ? Math.Min(current, value) : value;
	}

	private static void RemoveNull(OpenApiSchema schema)
	{
		schema.Type &= ~JsonSchemaType.Null;
	}

	private static void ApplyKnownValidator(IPropertyValidator validator, OpenApiSchema schema)
	{
		if (validator is IBetweenValidator range)
			ApplyRange(range, schema, validator is not IInclusiveBetweenValidator);

		if (validator is IEmailValidator && schema.Type?.HasFlag(JsonSchemaType.String) == true)
			schema.Format = "email";
	}

	private static void ApplyRange(IBetweenValidator validator, OpenApiSchema schema, bool exclusive)
	{
		if (TryToDecimal(validator.From) is { } minimum && ShouldReplaceBound(
				schema.ExclusiveMinimum ?? schema.Minimum,
				schema.ExclusiveMinimum is not null,
				minimum,
				exclusive,
				true))
		{
			schema.ExclusiveMinimum = null;
			schema.Minimum = null;

			if (exclusive)
				schema.ExclusiveMinimum = minimum.ToString(CultureInfo.InvariantCulture);
			else
				schema.Minimum = minimum.ToString(CultureInfo.InvariantCulture);
		}

		if (TryToDecimal(validator.To) is { } maximum && ShouldReplaceBound(
				schema.ExclusiveMaximum ?? schema.Maximum,
				schema.ExclusiveMaximum is not null,
				maximum,
				exclusive,
				false))
		{
			schema.ExclusiveMaximum = null;
			schema.Maximum = null;

			if (exclusive)
				schema.ExclusiveMaximum = maximum.ToString(CultureInfo.InvariantCulture);
			else
				schema.Maximum = maximum.ToString(CultureInfo.InvariantCulture);
		}
	}

	private static bool ShouldReplaceBound(
		string? current,
		bool currentExclusive,
		decimal value,
		bool exclusive,
		bool lowerBound)
	{
		if (!TryParseNumber(current, out var existing))
			return true;

		return (lowerBound ? value > existing : value < existing) ||
			(value == existing && exclusive && !currentExclusive);
	}
	private static bool TryParseNumber(string? value, out decimal number) =>
		decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number);

	private static decimal? TryToDecimal(object? value)
	{
		if (value is null)
			return null;

		try
		{
			return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
		}
		catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
		{
			return null;
		}
	}
}
