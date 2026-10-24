namespace GameAtlas.Application.Abstractions.Persistence;

public interface IPersistenceFailureClassifier
{
	bool IsGameSlugConflict(Exception exception);
	bool IsCategoryGroupNameConflict(Exception exception);
	bool IsCategoryNameConflict(Exception exception);
	bool IsMapNameConflict(Exception exception);
	bool IsMapStyleNameConflict(Exception exception);
}
