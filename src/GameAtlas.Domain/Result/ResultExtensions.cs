namespace GameAtlas.Domain.Result;

public static class ResultExtensions
{
	public static Result<IReadOnlyCollection<TResult>> Traverse<TSource, TResult>(
		this IEnumerable<TSource> source,
		Func<TSource, Result<TResult>> selector)
	{
		var values = new List<TResult>();
		var errors = new List<Error>();

		foreach (var item in source)
		{
			var result = selector(item);

			if (result.IsFailure)
			{
				errors.AddRange(result.Errors);
				continue;
			}

			values.Add(result.Value);
		}

		return errors.Count > 0 ? Result<IReadOnlyCollection<TResult>>.Failure([.. errors]) : values;
	}
}
