using FluentValidation;
using GameAtlas.Application.Behaviors;
using GameAtlas.Application.Games.CreateGame;
using GameAtlas.Domain.Result;

namespace GameAtlas.Application.Tests;

public class ValidationBehaviorTests
{
	[Fact]
	public async Task AggregatesFieldErrorsAndSkipsHandler()
	{
		var first = new InlineValidator<CreateGame>();
		var second = new InlineValidator<CreateGame>();
		var invoked = false;

		first.RuleFor(request => request.Name).NotEmpty();
		second.RuleFor(request => request.Slug).NotEmpty();
		var behavior = new ValidationBehavior<CreateGame, Result<GameDto>>([first, second]);
		var result = await behavior.Handle(new CreateGame(null, null, null), (_, _) =>
		{
			invoked = true;

			return ValueTask.FromResult(Result<GameDto>.Failure());
		}, CancellationToken.None);

		Assert.False(invoked);
		Assert.True(result.IsFailure);
		Assert.Equal(new[] { "Name", "Slug" }, result.Errors.Select(error => error.Property));
		Assert.All(result.Errors, error => Assert.Equal(ErrorType.Validation, error.Type));
	}

	[Fact]
	public async Task RunsValidatorsSequentiallyThenInvokesHandlerOnce()
	{
		var order = new List<int>();
		var validators = Enumerable.Range(1, 2).Select(index =>
		{
			var validator = new InlineValidator<CreateGame>();

			validator.RuleFor(request => request).CustomAsync(async (_, _, token) =>
			{
				order.Add(index);
				await Task.Yield();
				token.ThrowIfCancellationRequested();
				order.Add(index);
			});

			return validator;
		});

		var behavior = new ValidationBehavior<CreateGame, Result<GameDto>>(validators);
		var calls = 0;

		await behavior.Handle(new CreateGame("Game", "game", "cover"), (_, _) =>
		{
			calls++;
			order.Add(3);

			return ValueTask.FromResult(Result<GameDto>.Failure());
		}, CancellationToken.None);
		Assert.Equal(1, calls);
		Assert.Equal(new[] { 1, 1, 2, 2, 3 }, order);
	}

	[Fact]
	public async Task PropagatesCancellationWithoutCallingHandler()
	{
		using var source = new CancellationTokenSource();
		var behavior = new ValidationBehavior<CreateGame, Result<GameDto>>([]);

		source.Cancel();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
			await behavior.Handle(new CreateGame("Game", "game", "cover"), (_, _) =>
				throw new InvalidOperationException("Handler must not run."), source.Token));
	}

	[Fact]
	public async Task CreateGameValidatorChecksTrimmedValuesWithoutMutatingInput()
	{
		var command = new CreateGame($" {new string('n', 200)} ", " elden-ring ", " cover ");
		var validator = new CreateGameValidation();
		var result = await validator.ValidateAsync(command);

		Assert.True(result.IsValid);
		Assert.Equal(" elden-ring ", command.Slug);
		Assert.False((await validator.ValidateAsync(new CreateGame(null, "INVALID", " "))).IsValid);
	}

	[Theory]
	[InlineData(201, 1, 1, "Name")]
	[InlineData(1, 201, 1, "Slug")]
	[InlineData(1, 1, 2049, "Image")]
	public async Task CreateGameValidatorRejectsOverLimitMetadata(
		int nameLength, int slugLength, int imageLength, string property)
	{
		var command = new CreateGame(new string('n', nameLength), new string('s', slugLength),
			new string('i', imageLength));
		var result = await new CreateGameValidation().ValidateAsync(command);

		Assert.False(result.IsValid);
		Assert.Contains(result.Errors, error => error.PropertyName == property);
	}
}
