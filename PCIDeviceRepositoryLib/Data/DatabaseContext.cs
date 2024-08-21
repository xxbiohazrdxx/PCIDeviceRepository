using Microsoft.EntityFrameworkCore;
using RepositoryLib.Models;

namespace RepositoryLib.Data;

public class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
{
	public DbSet<Repository> Repository { get; set; }
	public DbSet<DeviceClass> Classes { get; set; }
	public DbSet<Vendor> Vendors { get; set; }

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
		});

		builder.Entity<Vendor>(entity =>
		{
			entity.ToContainer("Vendors")
				.HasNoDiscriminator()
				.HasKey(x => x.Id);

			entity.OwnsMany(
				w => w.Children,
				x =>
				{
					x.OwnsMany(
						y => y.Descendants,
						z =>
						{
							z.HasKey("DeviceVendorId", "DeviceId", "Id", "SubvendorId");
						});
				});
		});

		base.OnModelCreating(builder);
	}
}
