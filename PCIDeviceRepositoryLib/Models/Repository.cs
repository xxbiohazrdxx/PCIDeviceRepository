using System.Text.Json.Serialization;

namespace RepositoryLib.Models;

public class Repository
{
	public static Range VersionRange => new(new(11), new(0, true));

	[JsonIgnore]
	public string Id { get; private set; } = ".";
	public DateTime Version { get; set; } = DateTime.MinValue;
	public DateTime LastUpdate { get; set; } = DateTime.MinValue;
}
