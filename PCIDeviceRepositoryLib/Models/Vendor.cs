using System.Security.Cryptography;
using System.Text;

namespace RepositoryLib.Models;

public class Vendor()
{
	public static Range IdRange => new(new(0), new(4));
	public static Range NameRange => new(new(6), new(0, true));

	public string Id { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public List<Device> Devices { get; set; } = [];

	public string Hash { get; set; } = string.Empty;

	public async Task<string> GetHashAsync(CancellationToken token)
	{
		using var memoryStream = new MemoryStream();
		using var streamWriter = new StreamWriter(memoryStream);
		var stringBuilder = new StringBuilder();

		stringBuilder.Append(Id);
		stringBuilder.Append(Name);

		foreach (var device in Devices)
		{
			stringBuilder.Append(Id);
			stringBuilder.Append(Name);

			foreach (var subdevice in device.Subdevices)
			{
				stringBuilder.Append(subdevice.Id);
				stringBuilder.Append(subdevice.Name);
				stringBuilder.Append(subdevice.SubvendorId);
			}
		}

		await streamWriter.WriteAsync(stringBuilder, token);
		await streamWriter.FlushAsync(token);

		using var sha = SHA1.Create();
		var hash = sha.ComputeHash(memoryStream);
		return Convert.ToBase64String(hash);
	}
}
