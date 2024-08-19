using RepositoryLib.Models;

namespace RepositoryAPI.Dto;

public class DeviceDto
{
	public string VendorId { get; set; } = string.Empty;
	public string VendorName { get; set; } = string.Empty;
	public string DeviceId { get; set; } = string.Empty;
	public string DeviceName { get; set; } = string.Empty;
	public IEnumerable<SubdeviceDto> Subdevices { get; set; } = [];
}
