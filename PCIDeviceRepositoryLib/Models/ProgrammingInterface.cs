namespace RepositoryLib.Models;

public class ProgrammingInterface : DescendantModelBase, IParsable
{
	public static string ChunkRegex => throw new InvalidOperationException();
	public static Range IdRange => new(new(2), new(4));
	public static Range NameRange => new(new(6), new(0, true));
}
