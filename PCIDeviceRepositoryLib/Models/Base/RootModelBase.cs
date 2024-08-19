using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace RepositoryLib.Models;

public abstract class RootModelBase<T1, T2> : ModelBase where T1 : ChildBase<T2>, new() where T2 : DescendantBase, new()
{
	[JsonIgnore]
	public string Hash { get; private set; } = string.Empty;
	[JsonIgnore]
	public abstract List<T1> Children { get; set; }
	
	public void CalculateHash()
	{
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
			}
		}

		var hash = SHA1.HashData(Encoding.UTF8.GetBytes(stringBuilder.ToString()));
		Hash = Convert.ToBase64String(hash);
	}
}
