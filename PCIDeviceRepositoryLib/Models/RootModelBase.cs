using System.Security.Cryptography;
using System.Text;

namespace RepositoryLib.Models;

public class RootModelBase : ModelBase
{
	public virtual List<ChildModelBase> Children { get; set; } = [];

	public string Hash { get; set; } = string.Empty;

	public async Task CalculateHashAsync(CancellationToken token)
	{
		using var memoryStream = new MemoryStream();
		using var streamWriter = new StreamWriter(memoryStream);
		var stringBuilder = new StringBuilder();

		stringBuilder.Append(Id);
		stringBuilder.Append(Name);

		foreach (var child in Children)
		{
			stringBuilder.Append(Id);
			stringBuilder.Append(Name);

			foreach (var descendant in child.Descendants)
			{
				stringBuilder.Append(descendant.Id);
				stringBuilder.Append(descendant.Name);

				if (descendant is Subdevice subdevice)
				{
					stringBuilder.Append(subdevice.SubvendorId);
				}
			}
		}

		await streamWriter.WriteAsync(stringBuilder, token);
		await streamWriter.FlushAsync(token);

		using var sha = SHA1.Create();
		var hash = sha.ComputeHash(memoryStream);
		Hash = Convert.ToBase64String(hash);
	}
}
