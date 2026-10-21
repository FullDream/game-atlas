namespace GameAtlas.Domain.Result;

public interface IFailureFactory<TSelf>
	where TSelf : IFailureFactory<TSelf>
{
	static abstract TSelf Failure(params Error[] errors);
}