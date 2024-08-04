using Microsoft.Azure.Cosmos.Linq;
using Microsoft.EntityFrameworkCore;
using RepositoryLib.Models;

namespace RepositoryLib.Data;

public class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
{
	public DbSet<Repository> Repository { get; set; }

	public DbSet<DeviceClass> Classes { get; set; }
	public DbSet<DeviceSubclass> Subclasses { get; set; }
	public DbSet<ProgrammingInterface> ProgrammingInterfaces { get; set; }

	public DbSet<Vendor> Vendors { get; set; }
	public DbSet<Device> Devices { get; set; }
	public DbSet<Subdevice> Subdevices { get; set; }

	protected override void OnModelCreating(ModelBuilder builder)
	{
		builder.HasManualThroughput(1000);

		builder.Entity<Repository>(entity =>
		{
			entity.ToContainer("Repository")
				.HasNoDiscriminator()
				.HasKey(x => x.Id);

			entity.HasData([new()]);
		});

		builder.Entity<DeviceClass>(entity =>
		{
			entity.ToContainer("Classes")
				.HasNoDiscriminator()
				.HasKey(x => x.Id);

			entity.HasIndex(x => x.ClassId)
				.IsUnique();
		});

		builder.Entity<DeviceSubclass>(entity =>
		{
			entity.ToContainer("Subclasses")
				.HasNoDiscriminator()
				.HasKey(x => x.Id);

			entity.HasIndex(x => new
				{
					x.SubclassId,
					x.ClassId
				})
				.IsUnique();
		});

		builder.Entity<ProgrammingInterface>(entity =>
		{
			entity.ToContainer("ProgrammingInterfaces")
				.HasNoDiscriminator()
				.HasKey(x => x.Id);
		
			entity.HasIndex(x => new
				{
					x.ProgrammingInterfaceId,
					x.SubclassId
				})
				.IsUnique();
		});

		builder.Entity<Vendor>(entity =>
		{
			entity.ToContainer("Vendors")
				.HasNoDiscriminator()
				.HasKey(x => x.Id);

			entity.HasIndex(x => x.VendorId)
				.IsUnique();
		});

		builder.Entity<Device>(entity =>
		{
			entity.ToContainer("Devices")
				.HasNoDiscriminator()
				.HasKey(x => x.Id);

			entity.HasIndex(x => new
				{
					x.DeviceId,
					x.VendorId
				})
				.IsUnique();
		});

		builder.Entity<Subdevice>(entity =>
		{
			entity.ToContainer("Subdevices")
				.HasNoDiscriminator()
				.HasKey(x => x.Id);

			entity.HasIndex(x => new
				{ 
					x.SubvendorId,
					x.SubdeviceId, 
					x.DeviceId 
				})
				.IsUnique();
		});

		base.OnModelCreating(builder);
	}
}
