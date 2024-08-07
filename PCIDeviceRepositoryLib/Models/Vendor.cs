namespace RepositoryLib.Models;

public class Vendor : RootModelBase, IParsable
{
	public static string ChunkRegex => "^[a-f0-9]{4}";
	public static Range IdRange => new(new(0), new(4));
	public static Range NameRange => new(new(6), new(0, true));
}
