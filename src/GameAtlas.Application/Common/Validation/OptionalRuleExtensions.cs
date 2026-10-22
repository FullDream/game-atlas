using System.Linq.Expressions;
using FluentValidation;
using GameAtlas.Domain;

namespace GameAtlas.Application.Common.Validation;

public static class OptionalRuleExtensions
{
	public static void OptionalRuleFor<T, TProperty>(
		this AbstractValidator<T> validator,
		Expression<Func<T, Optional<TProperty>>> propertySelector,
		Func<IRuleBuilderInitial<T, TProperty>, IRuleBuilderOptions<T, TProperty>> configure,
		Expression<Func<TProperty, TProperty>>? normalize = null)
	{
		ArgumentNullException.ThrowIfNull(validator);
		ArgumentNullException.ThrowIfNull(propertySelector);
		ArgumentNullException.ThrowIfNull(configure);

		if (propertySelector.Body is not MemberExpression memberExpression)
			throw new ArgumentException("The selector must access an Optional member.", nameof(propertySelector));

		Expression value = Expression.Property(memberExpression, nameof(Optional<TProperty>.Value));

		if (normalize is not null)
			value = Expression.Invoke(normalize, value);

		var valueSelector = Expression.Lambda<Func<T, TProperty>>(value, propertySelector.Parameters);

		var selector = propertySelector.Compile();

		validator.When(
			instance => selector(instance).IsSpecified,
			() => configure(validator.RuleFor(valueSelector)).OverridePropertyName(memberExpression.Member.Name));
	}

	public static void OptionalRuleForEach<T, TElement>(
		this AbstractValidator<T> validator,
		Expression<Func<T, Optional<IReadOnlyCollection<TElement>>>> propertySelector,
		Func<IRuleBuilderInitialCollection<T, TElement>, IRuleBuilderOptions<T, TElement>> configure)
	{
		ArgumentNullException.ThrowIfNull(validator);
		ArgumentNullException.ThrowIfNull(propertySelector);
		ArgumentNullException.ThrowIfNull(configure);

		if (propertySelector.Body is not MemberExpression memberExpression)
			throw new ArgumentException("The selector must access an Optional member.", nameof(propertySelector));

		var valueSelector = Expression.Lambda<Func<T, IEnumerable<TElement>>>(
			Expression.Property(memberExpression, nameof(Optional<IReadOnlyCollection<TElement>>.Value)),
			propertySelector.Parameters);

		var selector = propertySelector.Compile();

		validator.When(
			instance => selector(instance).IsSpecified,
			() => configure(validator.RuleForEach(valueSelector)).OverridePropertyName(memberExpression.Member.Name));
	}
}
