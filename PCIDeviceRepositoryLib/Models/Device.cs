
using System.Text.Json.Serialization;

namespace RepositoryLib.Models;

public class Device : ChildBase<Subdevice>, IParsable
{
	public static string ChunkRegex => "^\t[^\t]";
	public static Range IdRange => new(new(1), new(5));
	public static Range NameRange => new(new(7), new(0, true));

	[JsonPropertyName("subdevices")]
	public override List<Subdevice> Descendants { get; set; } = [];
}
