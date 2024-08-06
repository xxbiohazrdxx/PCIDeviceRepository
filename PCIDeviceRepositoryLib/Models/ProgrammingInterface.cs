namespace RepositoryLib.Models;

public class ProgrammingInterface
{
	public static Range IdRange => new(new(2), new(4));
	public static Range NameRange => new(new(6), new(0, true));

	public string Id { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
}
