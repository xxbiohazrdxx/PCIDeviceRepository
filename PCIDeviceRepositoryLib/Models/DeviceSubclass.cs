
namespace RepositoryLib.Models;

public class DeviceSubclass : ChildBase<ProgrammingInterface>, IParsable
{
	public static string ChunkRegex => "^\t[^\t]";
	public static Range IdRange => new(new(1), new(3));
	public static Range NameRange => new(new(5), new(0, true));
}
