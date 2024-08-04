namespace RepositoryLib.Models;

public class DeviceClass
{
	public static Range IdRange => new(new(2), new(4));
	public static Range NameRange => new(new(6), new(0, true));

	public Guid Id { get; set; }
	public int ClassId { get; set; }
	public string Name { get; set; } = string.Empty;
}
