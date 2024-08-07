namespace RepositoryLib.Models;

public class ChildModelBase : ModelBase
{
	public virtual List<DescendantModelBase> Descendants { get; set; } = [];
}
