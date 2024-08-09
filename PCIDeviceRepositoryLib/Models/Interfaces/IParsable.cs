using System.Text.Json.Serialization;

namespace RepositoryLib.Models;

public interface IParsable
{
	public static abstract Range IdRange { get; }
	public static abstract Range NameRange { get; }
	public static abstract string ChunkRegex { get; }
}
