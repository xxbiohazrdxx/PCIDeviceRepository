using RepositoryLib.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using RepositoryLib.Data;
using RepositoryAPI.Dto;

namespace RepositoryAPI.Endpoints;

public static class DeviceEndpoint
{
	public static void MapDeviceEndpoints(this IEndpointRouteBuilder routes)
	{
		var group = routes.MapGroup("/api/devices").WithTags(nameof(Device));

		group.MapGet("/", async (DatabaseContext db, string? vendorId, string? deviceId) =>
		{
			return (await db.Vendors
				.Where(x => (vendorId == null) || x.Id == vendorId)
				.ToListAsync())
				.SelectMany(x => x.Children
					.Where(y => (deviceId == null) || y.Id == deviceId)
					.Select(y => new DeviceDto()
					{
						VendorId = x.Id,
						VendorName = x.Name,
						DeviceId = y.Id,
						DeviceName = y.Name,
						Subdevices = y.Descendants.Select(z => new SubdeviceDto()
						{
							SubvendorId = z.SubvendorId,
							SubdeviceId = z.Id,
							SubdeviceName = z.Name
						})
					}
				));
		})
		.WithName("GetAllDevices")
		.WithOpenApi();

		group.MapGet("/{vendorId}/{deviceId}", async Task<Results<Ok<DeviceDto>, NotFound>> (string vendorId, string deviceId, DatabaseContext db) =>
		{
			var vendor = await db.Vendors.AsNoTracking()
				.FirstOrDefaultAsync(x => x.Id == vendorId);
			var device = vendor?.Children.FirstOrDefault(x => x.Id == deviceId);

			if (vendor is null || device is null)
			{
				return TypedResults.NotFound();
			}

			return TypedResults.Ok(new DeviceDto()
			{
				VendorId = vendor.Id,
				VendorName = vendor.Name,
				DeviceId = device.Id,
				DeviceName = device.Name,
				Subdevices = device.Descendants.Select(x => new SubdeviceDto()
				{ 
					SubvendorId = x.SubvendorId, 
					SubdeviceId = x.Id, 
					SubdeviceName = x.Name 
				}),
			});
		})
		.WithName("GetDeviceyId")
		.WithOpenApi();
	}
}
