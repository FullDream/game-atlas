namespace GameAtlas.Api.Media;

public sealed record CreateMediaFolderRequest(string? Name, Guid? ParentId = null);
