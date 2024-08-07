using System.Security.Cryptography;
using System.Text;

namespace RepositoryLib.Models;

public class DeviceClass : RootModelBase, IParsable
{
	public static string ChunkRegex => "^C";
	public static Range IdRange => new(new(2), new(4));
	public static Range NameRange => new(new(6), new(0, true));
}
