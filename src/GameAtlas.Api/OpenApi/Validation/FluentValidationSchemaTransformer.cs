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
		if (context.ParameterDescription?.ModelMetadata is { ContainerType: { } contractType, PropertyName: { } name })
		{
			var sourceType = validationSourceResolver.Resolve(contractType);
			var sourceValidatorType = typeof(IValidator<>).MakeGenericType(sourceType);

			if (context.ApplicationServices.GetService(sourceValidatorType) is IValidator sourceValidator)
				ApplyRules(sourceValidator, name, schema);

			return Task.CompletedTask;
		}

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

			ApplyRules(validator, propertyInfo.Name, mutableSchema);
		}

		return Task.CompletedTask;
	}

	private static void ApplyRules(IValidator validator, string propertyName, OpenApiSchema schema)
	{
		foreach (var rule in validator.CreateDescriptor().GetRulesForMember(propertyName))
		{
			foreach (var component in rule.Components)
				ApplyValidator(component.Validator, schema);
		}
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

		if (validator is IComparisonValidator { MemberToCompare: null } comparison)
		{
			switch (comparison.Comparison)
			{
				case Comparison.GreaterThan:
				case Comparison.GreaterThanOrEqual:
					ApplyBound(schema, TryToDecimal(comparison.ValueToCompare),
						comparison.Comparison == Comparison.GreaterThan, true);
					break;
				case Comparison.LessThan:
				case Comparison.LessThanOrEqual:
					ApplyBound(schema, TryToDecimal(comparison.ValueToCompare),
						comparison.Comparison == Comparison.LessThan, false);
					break;
			}
		}

		if (validator is IEmailValidator && schema.Type?.HasFlag(JsonSchemaType.String) == true)
			schema.Format = "email";
	}

	private static void ApplyRange(IBetweenValidator validator, OpenApiSchema schema, bool exclusive)
	{
		ApplyBound(schema, TryToDecimal(validator.From), exclusive, true);
		ApplyBound(schema, TryToDecimal(validator.To), exclusive, false);
	}

	private static void ApplyBound(OpenApiSchema schema, decimal? bound, bool exclusive, bool lowerBound)
	{
		if (bound is not { } value)
			return;

		var current = lowerBound ? schema.ExclusiveMinimum ?? schema.Minimum : schema.ExclusiveMaximum ?? schema.Maximum;
		var currentExclusive = lowerBound ? schema.ExclusiveMinimum is not null : schema.ExclusiveMaximum is not null;

		if (!ShouldReplaceBound(current, currentExclusive, value, exclusive, lowerBound))
			return;

		if (lowerBound)
		{
			schema.ExclusiveMinimum = null;
			schema.Minimum = null;

			if (exclusive)
				schema.ExclusiveMinimum = value.ToString(CultureInfo.InvariantCulture);
			else
				schema.Minimum = value.ToString(CultureInfo.InvariantCulture);
		}
		else
		{
			schema.ExclusiveMaximum = null;
			schema.Maximum = null;

			if (exclusive)
				schema.ExclusiveMaximum = value.ToString(CultureInfo.InvariantCulture);
			else
				schema.Maximum = value.ToString(CultureInfo.InvariantCulture);
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
