using System.Text.Json.Serialization;

namespace GameAtlas.Api.Markers.Media;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ImageMediaRequest), "image")]
[JsonDerivedType(typeof(YouTubeVideoMediaRequest), "youtubeVideo")]
[JsonDerivedType(typeof(VideoMediaRequest), "video")]
public abstract record MarkerMediaRequest;
