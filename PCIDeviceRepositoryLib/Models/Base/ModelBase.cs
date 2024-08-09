using System.Text.Json.Serialization;

namespace RepositoryLib.Models;

public abstract class ModelBase
{
	[JsonPropertyOrder(-2)]
	public string Id { get; set; } = string.Empty;
	[JsonPropertyOrder(-1)]
	public string Name { get; set; } = string.Empty;
}

public abstract class ChildBase<T> : ModelBase where T : DescendantBase
{
	[JsonIgnore]
	public abstract List<T> Descendants { get; set; }
}

public abstract class DescendantBase : ModelBase { }