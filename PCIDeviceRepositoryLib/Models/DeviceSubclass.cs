namespace RepositoryLib.Models;

public class DeviceSubclass
{
	public static Range IdRange => new(new(1), new(2));
	public static Range NameRange => new(new(5), new(0, true));

	public Guid Id { get; set; }
	public Guid ClassId { get; set; }
	public DeviceClass Class { get; set; } = default!;
	public int SubclassId { get; set; }
	public string Name { get; set; } = string.Empty;
}
