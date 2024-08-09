using RepositoryLib.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using RepositoryLib.Data;

namespace RepositoryAPI.Endpoints;

public static class RepositoryEndpoint
{
	public static void MapRepositoryEndpoints(this IEndpointRouteBuilder routes)
	{
		var group = routes.MapGroup("/api/repository").WithTags(nameof(Repository));

		group.MapGet("/", async (DatabaseContext db) =>
		{
			return await db.Repository
				.SingleAsync();
		})
		.WithName("GetRepository")
		.WithOpenApi();
	}
}
