using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using GameAtlas.Application.Media.Inspection;
using GameAtlas.Application.Media.Storage;
using GameAtlas.Domain.Media;
using GameAtlas.Domain.Result;
using Microsoft.Extensions.Options;

namespace GameAtlas.Infrastructure.Media.Inspection;

public sealed class SvgInspector(IOptions<MediaLimits> options)
{
	private const string SvgNamespace = "http://www.w3.org/2000/svg";
	private static readonly HashSet<string> elements =
	[
		"svg", "g", "defs", "title", "desc", "path", "rect", "circle", "ellipse", "line", "polyline", "polygon",
		"linearGradient", "radialGradient", "stop", "clipPath", "mask"
	];

	private static readonly HashSet<string> numericAttributes =
	[
		"x", "y", "x1", "y1", "x2", "y2", "cx", "cy", "r", "rx", "ry", "fx", "fy", "width", "height",
		"opacity", "fill-opacity", "stroke-opacity", "stroke-width", "stroke-miterlimit", "stroke-dashoffset",
		"stop-opacity", "offset"
	];

	private static readonly Regex number = new(
		@"^[+-]?(?:[0-9]+(?:\.[0-9]*)?|\.[0-9]+)(?:[eE][+-]?[0-9]+)?%?$", RegexOptions.CultureInvariant,
		TimeSpan.FromMilliseconds(100));

	private static readonly Regex identifier = new(@"^[A-Za-z_][A-Za-z0-9_.-]*$",
		RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

	private static readonly Regex color = new(@"^(?:#[0-9A-Fa-f]{3}(?:[0-9A-Fa-f]{3})?|[A-Za-z]+)$",
		RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

	private static readonly Regex geometry = new(@"^[0-9eE+.,\s-]+$",
		RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

	private static readonly Regex pathData = new(@"^[MmZzLlHhVvCcSsQqTtAa0-9eE+.,\s-]+$",
		RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

	private static readonly Regex transform = new(
		@"^(?:(?:matrix|translate|scale|rotate|skewX|skewY)\([0-9eE+.,\s-]+\)\s*)+$",
		RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

	public Result<MediaInspection> Inspect(string path, CancellationToken cancellationToken)
	{
		var limits = options.Value;
		var settings = new XmlReaderSettings
		{
			DtdProcessing = DtdProcessing.Prohibit,
			XmlResolver = null,
			MaxCharactersInDocument = Math.Min(limits.MaximumXmlCharacters, limits.MaximumBytes),
			MaxCharactersFromEntities = 1,
			IgnoreComments = false,
			IgnoreProcessingInstructions = false
		};
		var elementCount = 0;
		var identifiers = new HashSet<string>(StringComparer.Ordinal);
		var references = new HashSet<string>(StringComparer.Ordinal);
		double? width = null;
		double? height = null;

		try
		{
			using var reader = XmlReader.Create(path, settings);

			while (reader.Read())
			{
				cancellationToken.ThrowIfCancellationRequested();

				if (reader.NodeType is XmlNodeType.ProcessingInstruction or XmlNodeType.DocumentType ||
					reader.Depth > limits.MaximumXmlDepth)
					return MediaErrors.InvalidContent;

				if (reader.NodeType != XmlNodeType.Element)
					continue;

				if (++elementCount > limits.MaximumXmlElements || reader.NamespaceURI != SvgNamespace ||
					!elements.Contains(reader.LocalName) || reader.AttributeCount > limits.MaximumXmlAttributes ||
					elementCount == 1 && reader.LocalName != "svg")
					return MediaErrors.InvalidContent;

				var root = elementCount == 1;

				while (reader.MoveToNextAttribute())
				{
					if (reader.Name == "xmlns" && reader.Value == SvgNamespace)
						continue;

					if (reader.NamespaceURI.Length != 0 || !ValidAttribute(reader.LocalName, reader.Value, references))
						return MediaErrors.InvalidContent;

					if (reader.LocalName == "id" && !identifiers.Add(reader.Value))
						return MediaErrors.InvalidContent;

					if (root && reader.LocalName == "width")
						width = Dimension(reader.Value);

					if (root && reader.LocalName == "height")
						height = Dimension(reader.Value);
				}

				reader.MoveToElement();
			}

			if (elementCount == 0 || !references.IsSubsetOf(identifiers) ||
				width > limits.MaximumImageSide || height > limits.MaximumImageSide)
				return MediaErrors.InvalidContent;

			return new MediaInspection("image/svg+xml", width, height);
		}
		catch (XmlException)
		{
			return MediaErrors.InvalidContent;
		}
		catch (RegexMatchTimeoutException)
		{
			return MediaErrors.InvalidContent;
		}
	}

	private static double? Dimension(string value) =>
		double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) &&
		double.IsFinite(number) && number > 0 ? number : null;

	private static bool ValidAttribute(string name, string value, HashSet<string> references)
	{
		if (numericAttributes.Contains(name))
			return number.IsMatch(value);

		if (name is "fill" or "stroke" or "stop-color")
		{
			if (value.StartsWith("url(#", StringComparison.Ordinal) && value.EndsWith(')'))
			{
				var reference = value[5..^1];

				if (!identifier.IsMatch(reference))
					return false;

				references.Add(reference);

				return true;
			}

			return color.IsMatch(value);
		}

		if (name is "clip-path" or "mask")
		{
			if (!value.StartsWith("url(#", StringComparison.Ordinal) || !value.EndsWith(')'))
				return value == "none";

			var reference = value[5..^1];

			if (!identifier.IsMatch(reference))
				return false;

			references.Add(reference);

			return true;
		}

		return name switch
		{
			"id" => identifier.IsMatch(value),
			"d" => pathData.IsMatch(value),
			"viewBox" or "points" or "stroke-dasharray" => geometry.IsMatch(value) || value == "none",
			"transform" or "gradientTransform" => transform.IsMatch(value),
			"fill-rule" or "clip-rule" => value is "nonzero" or "evenodd",
			"stroke-linecap" => value is "butt" or "round" or "square",
			"stroke-linejoin" => value is "miter" or "round" or "bevel",
			"gradientUnits" or "clipPathUnits" or "maskUnits" or "maskContentUnits" =>
				value is "userSpaceOnUse" or "objectBoundingBox",
			"spreadMethod" => value is "pad" or "reflect" or "repeat",
			"preserveAspectRatio" => value is "none" or "xMidYMid" or "xMidYMid meet" or "xMidYMid slice",
			_ => false
		};
	}
}
