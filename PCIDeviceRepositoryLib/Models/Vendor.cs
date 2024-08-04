namespace RepositoryLib.Models;

public class Vendor()
{
	public static Range IdRange => new(new(0), new(4));
	public static Range NameRange => new(new(6), new(0, true));

	public Guid Id { get; set; }
	public int VendorId { get; set; }
	public string Name { get; set; } = string.Empty;
}
