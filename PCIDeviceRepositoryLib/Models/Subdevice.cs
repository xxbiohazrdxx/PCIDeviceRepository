namespace RepositoryLib.Models;

public class Subdevice : DescendantModelBase, IParsable
{
	public static string ChunkRegex => throw new InvalidOperationException();
	public static Range SubvendorIdRange => new(new(2), new(6));
	public static Range IdRange => new(new(7), new(11));
	public static Range NameRange => new(new(13), new(0, true));

	public string SubvendorId { get; set; } = string.Empty;
}
