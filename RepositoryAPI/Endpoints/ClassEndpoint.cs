using RepositoryLib.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using RepositoryLib.Data;

namespace RepositoryAPI.Endpoints;

public static class ClassEndpoint
{
	public static void MapClassEndpoints(this IEndpointRouteBuilder routes)
	{
		var group = routes.MapGroup("/api/classes").WithTags(nameof(DeviceClass));

		group.MapGet("/", async (DatabaseContext db) =>
		{
			return await db.Classes.ToListAsync();
		})
		.WithName("GetAllClasses")
		.WithOpenApi();

		group.MapGet("/{id}", async Task<Results<Ok<DeviceClass>, NotFound>> (string id, DatabaseContext db) =>
		{
			return await db.Classes.AsNoTracking()
				.FirstOrDefaultAsync(model => model.Id == id)
				is DeviceClass model
					? TypedResults.Ok(model)
					: TypedResults.NotFound();
		})
		.WithName("GetClassById")
		.WithOpenApi();
	}
}
