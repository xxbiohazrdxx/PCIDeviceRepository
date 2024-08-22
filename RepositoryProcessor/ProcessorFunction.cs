using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RepositoryLib.Data;
using RepositoryLib.Models;
using RepositoryProcessor.Configuration;
using System.Text.RegularExpressions;

namespace RepositoryProcessor;

public partial class ProcessorFunction(ILoggerFactory loggerFactory, IDbContextFactory<DatabaseContext> dbContextFactory, IOptions<FunctionConfiguration> configuration)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<ProcessorFunction>();
	private readonly FunctionConfiguration _configuration = configuration.Value;

	[Function("DeviceProcessorFunction")]
    public async Task Run([TimerTrigger("0 0 * * *", RunOnStartup = true)] TimerInfo timer, CancellationToken token)
    {
        _logger.LogInformation("Timer trigger function executed at: {now}", DateTime.Now);
        
        if (timer.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next timer schedule at: {next}", timer.ScheduleStatus.Next);
        }

		using (var context = await dbContextFactory.CreateDbContextAsync(token))
		{
			await context.Database.EnsureCreatedAsync(token);
		}

		using var client = new HttpClient();
		var repositoryContent = (await client.GetStringAsync(_configuration.RepositoryUrl, token))
			.Split('\n', StringSplitOptions.RemoveEmptyEntries);

		// Get the version from the repository file
		var versionString = repositoryContent[3];
		if (versionString.StartsWith("#\tVersion: ") && DateTime.TryParse(versionString[Repository.VersionRange], out var version))
		{
			_logger.LogInformation("Detected repository version {version}", version.ToString("yyyy.MM.dd"));

			using var context = await dbContextFactory.CreateDbContextAsync(token);
			var repository = await context.Repository.SingleOrDefaultAsync(token) ?? new();

			if (repository.Version == version)
			{
				_logger.LogInformation("Repository version matches existing database version. No processing is required.");
				return;
			}
		}
		else
		{
			_logger.LogCritical("Verion string was not found or not in the correct format.");
			throw new InvalidDataException("Verion string was not found or not in the correct format.");
		}

		// Remove comments
		var repositoryData = repositoryContent.Where(x => !x.StartsWith('#'));

		// Validate the lines
#pragma warning disable SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.
		if (!repositoryData.All(x => (
			Regex.IsMatch(x, @"^C [0-9a-f]{2}  .+$")   ||			// Class
			Regex.IsMatch(x, @"^\t[0-9a-f]{2}  .+$")   ||			// Subclass
			Regex.IsMatch(x, @"^\t\t[0-9a-f]{2}  .+$") ||			// Programming interface
			Regex.IsMatch(x, @"^[0-9a-f]{4}  .+$")     ||			// Vendor
			Regex.IsMatch(x, @"^\t[0-9a-f]{4}  .+$")   ||			// Device
			Regex.IsMatch(x, @"^\t\t[0-9a-f]{4} [0-9a-f]{4}  .+$")	// Subdevice
		)))
		{
			_logger.LogCritical("One or more lines in the repository definition file did not pass validation.");
			throw new InvalidDataException("One or more lines in the repository definition file did not pass validation.");
		}
#pragma warning restore SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.

		var repositoryDevices = repositoryData.TakeWhile(x => !x.StartsWith('C'));
		var repositoryClasses = repositoryData.SkipWhile(x => !x.StartsWith('C'));

		_logger.LogInformation("Retrieved {device} lines of devices and {class} lines of classes",
			repositoryDevices.Count(),
			repositoryClasses.Count());

		await Parse<DeviceClass, DeviceSubclass, ProgrammingInterface>(repositoryClasses, token);
		await Parse<Vendor, Device, Subdevice>(repositoryDevices, token);

		using (var context = await dbContextFactory.CreateDbContextAsync(token))
		{
			var repository = await context.Repository.SingleAsync(token);
			repository.Version = version;
			repository.LastUpdate = DateTime.UtcNow;
			await context.SaveChangesAsync(token);

			_logger.LogInformation("Processing complete, updated version to {version}", version);
		}
	}

	private async Task Parse<T1, T2, T3>(IEnumerable<string> vendorLines, CancellationToken token) 
		where T1 : RootModelBase<T2, T3>, IParsable, new() 
		where T2 : ChildBase<T3>, IParsable, new() 
		where T3 : DescendantBase, IParsable, new()
	{
		foreach (var rootChunk in Chunk(vendorLines, T1.ChunkRegex))
		{
			var root = new T1()
			{
				Id = rootChunk.First()[T1.IdRange],
				Name = rootChunk.First()[T1.NameRange]
			};

			foreach (var children in Chunk(rootChunk.Skip(1), T2.ChunkRegex))
			{
				var child = new T2()
				{
					Id = children.First()[T2.IdRange],
					Name = children.First()[T2.NameRange]
				};

				foreach (var descendant in children.Skip(1))
				{
					var newDescendant = new T3()
					{
						Id = descendant[T3.IdRange],
						Name = descendant[T3.NameRange]
					};

					if (newDescendant is Subdevice subdevice)
					{
						subdevice.SubvendorId = descendant[Subdevice.SubvendorIdRange];
					}

					child.Descendants.Add(newDescendant);
				}

				root.Children.Add(child);
			}

			root.CalculateHash();

			_logger.LogInformation("Parsed {baseType} {id} - {name}. Contains {children} children.", 
				typeof(T1).Name,
				root.Id,
				root.Name,
				root.Children.Count);

			using var context = await dbContextFactory.CreateDbContextAsync(token);

			var existing = await context.Set<T1>().FindAsync([root.Id], token);
			if (existing is not null && existing.Hash == root.Hash)
			{
				_logger.LogInformation("Skipping {entity} with id {id} as it already exists and matches database.",
					typeof(T1).Name,
					root.Id);
				continue;
			}
			else if (existing is null)
			{
				_logger.LogInformation("Adding {entity} to database as it does not already exist.", typeof(T1).Name);
				context.Set<T1>().Add(root);
			}
			else if (existing.Hash != root.Hash)
			{
				
				_logger.LogInformation("Updating {entity} in database as the hash has changed.", typeof(T1).Name);
				context.Entry(existing).State = EntityState.Detached;
				context.Set<T1>().Update(root);
			}

			await context.SaveChangesAsync(token);
		}

		_logger.LogInformation("Completed parsing of {basteType}.", typeof(T1).Name);
	}

	private static IEnumerable<IEnumerable<string>> Chunk(IEnumerable<string> lines, string regex)
	{
		List<string> chunks = [];
		foreach (var currentLine in lines)
		{
			if (Regex.IsMatch(currentLine, regex) && chunks.Count > 0)
			{
				yield return chunks;
				chunks = [];
			}

			chunks.Add(currentLine);
		}

		if (chunks.Count > 0)
		{
			yield return chunks;
		}
	}
}
