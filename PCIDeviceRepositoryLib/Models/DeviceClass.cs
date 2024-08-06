using System.Security.Cryptography;
using System.Text;

namespace RepositoryLib.Models;

public class DeviceClass
{
	public static Range IdRange => new(new(2), new(4));
	public static Range NameRange => new(new(6), new(0, true));

	public string Id { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public List<DeviceSubclass> Subclasses { get; set; } = [];

	public string Hash { get; set; } = string.Empty;

	public async Task<string> GetHashAsync(CancellationToken token)
	{
		//using var memoryStream = new MemoryStream();
		//using var streamWriter = new StreamWriter(memoryStream);
		var stringBuilder = new StringBuilder();

		stringBuilder.Append(Id);
		stringBuilder.Append(Name);

		foreach (var subclass in Subclasses)
		{
			stringBuilder.Append(subclass.Id);
			stringBuilder.Append(subclass.Name);

			foreach (var programmingInterface in subclass.ProgrammingInterfaces)
			{
				stringBuilder.Append(programmingInterface.Id);
				stringBuilder.Append(programmingInterface.Name);
			}
		}

		var bytes = Encoding.UTF8.GetBytes(stringBuilder.ToString());

		//await streamWriter.WriteAsync(stringBuilder, token);
		//await streamWriter.FlushAsync(token);

		using var sha = SHA1.Create();
		var hash = sha.ComputeHash(bytes);
		//var hash = sha.ComputeHash(memoryStream);
		return Convert.ToBase64String(hash);
	}
}
