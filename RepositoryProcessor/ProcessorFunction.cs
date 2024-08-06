using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RepositoryLib.Data;
using RepositoryLib.Models;
using RepositoryProcessor.Configuration;
using System.Globalization;

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
			await context.Database.EnsureDeletedAsync(token);
			await context.Database.EnsureCreatedAsync(token);
		}

		using var client = new HttpClient();
		var repositoryContent = (await client.GetStringAsync(_configuration.RepositoryUrl, token))
			.Split('\n', StringSplitOptions.RemoveEmptyEntries);

		var versionString = repositoryContent
			.Skip(3)
			.Take(1)
			.Single();

		#region Version
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
			_logger.LogCritical("Unable to detect file version.");
			throw new InvalidDataException("Verion string was not found or not in the correct format.");
		}
		#endregion

		var repositoryDevices = repositoryContent
			.Where(x => !x.StartsWith('#'))
			.TakeWhile(x => !x.StartsWith('C'));
		var repositoryClasses = repositoryContent
			.Where(x => !x.StartsWith('#'))
			.SkipWhile(x => !x.StartsWith('C'));

		_logger.LogInformation("Retrieved {device} lines of devices and {class} lines of classes",
			repositoryDevices.Count(),
			repositoryClasses.Count());

		await ParseClasses(repositoryClasses, token);
		await ParseDevices(repositoryDevices, token);

		using (var context = await dbContextFactory.CreateDbContextAsync(token))
		{
			var repository = await context.Repository.SingleAsync(token);
			repository.Version = version;
			repository.LastUpdate = DateTime.UtcNow;
			repository.Refreshing = false;
			await context.SaveChangesAsync(token);

			_logger.LogInformation("Processing complete, updated version to {version}", version);
		}
	}

	private async Task ParseClasses(IEnumerable<string> repositoryClasses, CancellationToken token)
	{
		DeviceClass devClass = null!;
		foreach (var currentClass in repositoryClasses)
		{
			// Class
			if (currentClass.StartsWith('C'))
			{
				if (devClass is not null)
				{
					devClass.Hash = await devClass.GetHashAsync(token);

					using var context = await dbContextFactory.CreateDbContextAsync(token);

					if (await context.Classes.AnyAsync(x => (x.Id == devClass.Id) && (x.Hash == devClass.Hash), token))
					{
						_logger.LogInformation("Finished parsing class {class}, identitcal class already exists in database.", devClass.Id);
					}
					else
					{
						context.Classes.Add(devClass);
						await context.SaveChangesAsync(token);
					}
				}

				var id = currentClass[DeviceClass.IdRange];
				var name = currentClass[DeviceClass.NameRange];

				if (!int.TryParse(id, NumberStyles.HexNumber, null, out _))
				{
					_logger.LogWarning("Detected device class but id could not be parsed: {line}", currentClass);
					continue;
				}

				devClass = new()
				{
					Id = id,
					Name = name
				};
			}
			// Programming interface
			else if (currentClass.StartsWith("\t\t"))
			{
				var id = currentClass[ProgrammingInterface.IdRange];
				var name = currentClass[ProgrammingInterface.NameRange];

				if (!int.TryParse(id, NumberStyles.HexNumber, null, out _))
				{
					_logger.LogWarning("Detected programming interface but id could not be parsed: {line}", currentClass);
					continue;
				}

				devClass.Subclasses[^1].ProgrammingInterfaces.Add(new()
				{
					Id = id,
					Name = name
				});
			}
			// Subclass
			else if (currentClass.StartsWith('\t'))
			{
				var id = currentClass[DeviceSubclass.IdRange];
				var name = currentClass[DeviceSubclass.NameRange];

				if (!int.TryParse(id, NumberStyles.HexNumber, null, out _))
				{
					_logger.LogWarning("Detected device subclass but id could not be parsed: {line}", currentClass);
					continue;
				}

				devClass.Subclasses.Add(new()
				{
					Id = id,
					Name = name
				});
			}
			else
			{
				_logger.LogError("Unexpected format when parsing classes: {line}", currentClass);
			}
		}

		_logger.LogInformation("Completed parsing of classes.");
	}

	private async IAsyncEnumerable<IEnumerable<string>> ChunkClasses(List<string> rawClasses, CancellationToken token)
	{
		List<string> classChunks = [];
		foreach (var currentLine in rawClasses)
		{
			if (currentLine.StartsWith('C') && classChunks.Count > 0)
			{
				yield return classChunks;
				classChunks = [];
			}

			classChunks.Add(currentLine);
		}
	}

	private async Task ParseDevices(IEnumerable<string> repositoryDevices, CancellationToken token)
	{
		Vendor vendor = null!;
		foreach (var currentLine in repositoryDevices)
		{
			// Subdevice
			if (currentLine.StartsWith("\t\t"))
			{
				var id = currentLine[Subdevice.IdRange];
				var name = currentLine[Subdevice.NameRange];

				if (!int.TryParse(id, NumberStyles.HexNumber, null, out _))
				{
					_logger.LogWarning("Detected subdevice but subdevice id could not be parsed: {line}", currentLine);
					continue;
				}

				var subvendorId = currentLine[Subdevice.SubvendorIdRange];
				if (!int.TryParse(subvendorId, NumberStyles.HexNumber, null, out _))
				{
					_logger.LogWarning("Detected subdevice but subvendor id could not be parsed: {line}", currentLine);
					continue;
				}

				vendor.Devices[^1].Subdevices.Add(new()
				{
					Id = id,
					Name = name,
					SubvendorId = subvendorId
				});
			}
			// Device
			else if (currentLine.StartsWith('\t'))
			{
				var id = currentLine[Device.IdRange];
				var name = currentLine[Device.NameRange];

				if (!int.TryParse(id, NumberStyles.HexNumber, null, out _))
				{
					_logger.LogWarning("Detected device but device id could not be parsed: {line}", currentLine);
					continue;
				}

				vendor.Devices.Add(new()
				{
					Id = id,
					Name = name
				});
			}
			// Vendor
			else
			{
				if (vendor is not null)
				{
					vendor.Hash = await vendor.GetHashAsync(token);

					using var context = await dbContextFactory.CreateDbContextAsync(token);
					context.Vendors.Add(vendor);
					await context.SaveChangesAsync(token);
				}

				var id = currentLine[Vendor.IdRange];
				var name = currentLine[Vendor.NameRange];

				if (!int.TryParse(id, NumberStyles.HexNumber, null, out _))
				{
					_logger.LogWarning("Detected vendor but vendor id could not be parsed: {line}", currentLine);
					continue;
				}

				vendor = new()
				{
					Id = id,
					Name = name
				};
			}
		}

		_logger.LogInformation("Completed parsing of devices.");
	}
}
