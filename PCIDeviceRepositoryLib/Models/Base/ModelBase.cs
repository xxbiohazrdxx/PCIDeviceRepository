namespace RepositoryLib.Models;

public abstract class ModelBase
{
	public string Id { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
}

public abstract class ChildBase<T> : ModelBase where T : DescendantBase
{
	public List<T> Descendants { get; set; } = [];
}

public abstract class DescendantBase : ModelBase { }