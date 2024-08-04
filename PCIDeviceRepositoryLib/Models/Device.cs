namespace RepositoryLib.Models;

public class Device()
{
	public static Range IdRange => new(new(1), new(5));
	public static Range NameRange => new(new(7), new(0, true));

	public Guid Id { get; set; }
	public Guid VendorId { get; set; }
	public Vendor Vendor { get; set; } = default!;
	public int DeviceId { get; set; }
	public string Name { get; set; } = string.Empty;
}
