namespace RepositoryLib.Models;

public class DeviceSubclass
{
	public static Range IdRange => new(new(1), new(3));
	public static Range NameRange => new(new(5), new(0, true));

	public string Id { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public List<ProgrammingInterface> ProgrammingInterfaces { get; set; } = [];
}
