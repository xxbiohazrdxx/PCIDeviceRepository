namespace RepositoryLib.Models;

public class Repository
{
	public static Range VersionRange => new(new(11), new(0, true));

	public Guid Id { get; private set; } = new();
	public DateTime Version { get; set; } = DateTime.MinValue;
	public DateTime LastUpdate { get; set; } = DateTime.MinValue;
	public bool Refreshing { get; set; } = true;
}
