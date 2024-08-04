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

		#region Classes
		DeviceClass deviceClass = default!;
		DeviceSubclass deviceSubclass = default!;
		int i = 0;
		foreach (var currentClass in repositoryClasses)
		{
			_logger.LogInformation("Class {i} of {totalClasses}", i++, repositoryClasses.Count());
			using var context = await dbContextFactory.CreateDbContextAsync(token);

			// Class
			if (currentClass.StartsWith('C'))
			{
				if (!int.TryParse(currentClass[DeviceClass.IdRange], NumberStyles.HexNumber, null, out var classId))
				{
					_logger.LogWarning("Detected device class but id could not be parsed: {line}", currentClass);
					continue;
				}

				deviceClass = (await context.Classes.SingleOrDefaultAsync(x => x.ClassId == classId, token)) ?? new()
				{
					ClassId = classId,
					Name = currentClass[DeviceClass.NameRange]
				};

				if (context.Entry(deviceClass).State == EntityState.Detached)
				{
					await context.Classes.AddAsync(deviceClass, token);
					await context.SaveChangesAsync(token);

					_logger.LogTrace("Parsed device class {class} - {name}", deviceClass.ClassId, deviceClass.Name);
				}
			}
			// Programming interface
			else if(currentClass.StartsWith("\t\t"))
			{
				if (!int.TryParse(currentClass[ProgrammingInterface.IdRange], NumberStyles.HexNumber, null, out var programmingInterfaceId))
				{
					_logger.LogWarning("Detected programming interface but id could not be parsed: {line}", currentClass);
					continue;
				}

				var programmingInterface = (await context.ProgrammingInterfaces.SingleOrDefaultAsync(x => 
					(x.SubclassId == deviceSubclass.Id) && 
					(x.ProgrammingInterfaceId == programmingInterfaceId), token)) ?? new()
				{
					Subclass = deviceSubclass,
					ProgrammingInterfaceId = programmingInterfaceId,
					Name = currentClass[ProgrammingInterface.NameRange]
				};

				if (context.Entry(programmingInterface).State == EntityState.Detached)
				{
					context.Attach(programmingInterface.Subclass);
					await context.ProgrammingInterfaces.AddAsync(programmingInterface, token);
					await context.SaveChangesAsync(token);

					_logger.LogTrace("Parsed programming interface {class}:{subclass}:{programmingInterface} - {name}",
						programmingInterface.Subclass.ClassId,
						$"{programmingInterface.Subclass.SubclassId:X4}",
						$"{programmingInterface.ProgrammingInterfaceId:X4}",
						programmingInterface.Name);
				}
			}
			// Subclass
			else if(currentClass.StartsWith('\t'))
			{
				if (!int.TryParse(currentClass[DeviceSubclass.IdRange], NumberStyles.HexNumber, null, out var subclassId))
				{
					_logger.LogWarning("Detected device subclass but id could not be parsed: {line}", currentClass);
					continue;
				}

				deviceSubclass = (await context.Subclasses.SingleOrDefaultAsync(x => 
					(x.ClassId == deviceClass.Id) && 
					(x.SubclassId == subclassId), token)) ?? new()
				{
					Class = deviceClass,
					SubclassId = subclassId,
					Name = currentClass[DeviceSubclass.NameRange]
				};

				if (context.Entry(deviceSubclass).State == EntityState.Detached)
				{
					context.Attach(deviceSubclass.Class);
					await context.Subclasses.AddAsync(deviceSubclass, token);
					await context.SaveChangesAsync(token);

					_logger.LogTrace("Parsed device subclass {class}:{subclass} - {name}",
						$"{deviceSubclass.Class.ClassId:X4}",
						$"{deviceSubclass.SubclassId:X4}",
						deviceSubclass.Name);
				}
			}
			else
			{
				_logger.LogError("Unexpected format when parsing classes: {line}", currentClass);
			}
		}
		#endregion

		#region Devices
		Vendor vendor = null!;
		Device device = null!;
		i = 0;
		foreach (var currentLine in repositoryDevices)
		{
			_logger.LogInformation("Device {i} of {totalDevices}", i++, repositoryDevices.Count());
			using var context = await dbContextFactory.CreateDbContextAsync(token);

			// Subdevice
			if (currentLine.StartsWith("\t\t"))
			{
				if (!int.TryParse(currentLine[Subdevice.IdRange], NumberStyles.HexNumber, null, out var subdeviceId))
				{
					_logger.LogWarning("Detected subdevice but subdevice id could not be parsed: {line}", currentLine);
					continue;
				}

				if (!int.TryParse(currentLine[Subdevice.SubvendorIdRange], NumberStyles.HexNumber, null, out var subvendorId))
				{
					_logger.LogWarning("Detected subdevice but subvendor id could not be parsed: {line}", currentLine);
					continue;
				}

				var subdevice = (await context.Subdevices.SingleOrDefaultAsync(x => 
					(x.DeviceId == device.Id) && 
					(x.SubdeviceId == subdeviceId), token)) ?? new()
				{
					Device = device,
					SubvendorId = subvendorId,
					SubdeviceId = subdeviceId,
					Name = currentLine[Subdevice.NameRange]
				};

				if (context.Entry(subdevice).State == EntityState.Detached)
				{
					context.Attach(subdevice.Device);
					await context.Subdevices.AddAsync(subdevice, token);
					await context.SaveChangesAsync(token);

					_logger.LogTrace("Parsed subdevice {vendor}:{device}:{subvendor}:{subdevice} - {name}",
						$"{subdevice.Device.Vendor.VendorId:X4}",
						$"{subdevice.Device.DeviceId:X4}",
						$"{subdevice.SubvendorId:X4}",
						$"{subdevice.SubdeviceId:X4}",
						subdevice.Name);
				}
			}
			// Device
			else if (currentLine.StartsWith('\t'))
			{
				if(!int.TryParse(currentLine[Device.IdRange], NumberStyles.HexNumber, null, out var deviceId))
				{
					_logger.LogWarning("Detected device but device id could not be parsed: {line}", currentLine);
					continue;
				}

				device = (await context.Devices.SingleOrDefaultAsync(x =>
					(x.VendorId == vendor.Id) &&
					(x.DeviceId == deviceId), token)) ?? new()
				{
					Vendor = vendor,
					DeviceId = deviceId,
					Name = currentLine[Device.NameRange]
				};

				if (context.Entry(device).State == EntityState.Detached)
				{
					context.Attach(device.Vendor);
					await context.Devices.AddAsync(device, token);
					await context.SaveChangesAsync(token);

					_logger.LogTrace("Parsed device {vendor}:{device} - {name}",
						$"{device.Vendor.VendorId:X4}",
						$"{device.DeviceId:X4}",
						device.Name);
				}
			}
			// Vendor
			else
			{
				if (!int.TryParse(currentLine[Vendor.IdRange], NumberStyles.HexNumber, null, out var vendorId))
				{
					_logger.LogWarning("Detected vendor but vendor id could not be parsed: {line}", currentLine);
					continue;
				}

				vendor = (await context.Vendors.SingleOrDefaultAsync(x => x.VendorId == vendorId, token)) ?? new()
				{
					VendorId = vendorId,
					Name = currentLine[Vendor.NameRange]
				};

				if (context.Entry(vendor).State == EntityState.Detached)
				{
					await context.Vendors.AddAsync(vendor, token);
					await context.SaveChangesAsync(token);

					_logger.LogTrace("Parsed vendor {vendor} - {name}",
						$"{vendor.VendorId:X4}",
						vendor.Name);
				}
			}
		}
		#endregion

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
}
