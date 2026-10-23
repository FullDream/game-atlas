using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using GameAtlas.Api.Markers.Media;
using GameAtlas.Application.Markers.Media;
using GameAtlas.Application.Markers.Media.Image;
using GameAtlas.Application.Markers.Media.Video;
using GameAtlas.Application.Markers.Media.YouTubeVideo;

namespace GameAtlas.Api.Markers;

internal static class MarkerMediaJsonTypeInfoModifier
{
	internal static void Modify(JsonTypeInfo typeInfo)
	{
		if (typeInfo.Type == typeof(MarkerMediaRequest))
		{
			typeInfo.CreateObject = () => throw new JsonException("A marker media type discriminator is required.");

			return;
		}

		if (typeInfo.Type != typeof(MarkerMediaDto))
			return;

		typeInfo.PolymorphismOptions = new JsonPolymorphismOptions
		{
			TypeDiscriminatorPropertyName = "type",
			DerivedTypes =
			{
				new JsonDerivedType(typeof(ImageMediaDto), "image"),
				new JsonDerivedType(typeof(YouTubeVideoMediaDto), "youtubeVideo"),
				new JsonDerivedType(typeof(VideoMediaDto), "video")
			}
		};
	}
}
