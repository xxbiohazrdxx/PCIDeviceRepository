namespace RepositoryLib.Models;

public class Device : ChildModelBase, IParsable
{
	public static string ChunkRegex => "^\t[^\t]";
	public static Range IdRange => new(new(1), new(5));
	public static Range NameRange => new(new(7), new(0, true));
}
