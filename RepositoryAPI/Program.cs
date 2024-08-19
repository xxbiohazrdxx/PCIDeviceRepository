using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RepositoryLib.Configuration;
using RepositoryLib.Data;
using RepositoryAPI.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddOptions<DatabaseConfiguration>()
	.Configure<IConfiguration>((settings, configuration) =>
	{
		configuration.GetSection("DatabaseConfiguration").Bind(settings);
	});

builder.Services.AddDbContextFactory<DatabaseContext>((IServiceProvider serviceProvider, DbContextOptionsBuilder options) =>
{
	var databaseConfiguration = serviceProvider.GetRequiredService<IOptions<DatabaseConfiguration>>().Value;
	options.UseCosmos(databaseConfiguration.Uri, databaseConfiguration.PrimaryKey, databaseConfiguration.DatabaseName);
#if DEBUG
	options.EnableSensitiveDataLogging();
#endif
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapVendorEndpoints();
app.MapDeviceEndpoints();
app.MapClassEndpoints();
app.MapRepositoryEndpoints();

app.Run();
