namespace GameAtlas.Application.Abstractions.Persistence;

public interface IPersistenceFailureClassifier
{
	bool IsGameSlugConflict(Exception exception);
}
