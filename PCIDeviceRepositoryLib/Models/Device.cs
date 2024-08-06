using System.Text;

namespace RepositoryLib.Models;

public class Device()
{
	public static Range IdRange => new(new(1), new(5));
	public static Range NameRange => new(new(7), new(0, true));

	public string Id { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public List<Subdevice> Subdevices { get; set; } = [];
}
