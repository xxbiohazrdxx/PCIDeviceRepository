using RepositoryLib.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using RepositoryLib.Data;

namespace RepositoryAPI.Endpoints;

public static class VendorEndpoint
{
	public static void MapVendorEndpoints (this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/vendors").WithTags(nameof(Vendor));

        group.MapGet("/", async (DatabaseContext db) =>
        {
            return await db.Vendors.ToListAsync();
        })
        .WithName("GetAllVendors")
        .WithOpenApi();

        group.MapGet("/{id}", async Task<Results<Ok<Vendor>, NotFound>> (string id, DatabaseContext db) =>
        {
            return await db.Vendors.AsNoTracking()
                .FirstOrDefaultAsync(model => model.Id == id)
                is Vendor model
                    ? TypedResults.Ok(model)
                    : TypedResults.NotFound();
        })
        .WithName("GetVendorById")
        .WithOpenApi();
    }
}
