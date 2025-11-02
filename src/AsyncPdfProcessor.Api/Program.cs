using AsyncPdfProcessor.Infrastructure;
using Hangfire;
using Hangfire.SqlServer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddInfrastructureServices(builder.Configuration);

var hangfireConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddHangfire(config => config
	.UseRecommendedSerializerSettings()
	// MSSQL'i depolama olarak ayarlar
	.UseSqlServerStorage(hangfireConnectionString, new SqlServerStorageOptions
	{
		SchemaName = "HangFire", // Hangfire tablolarý için ayrý schema
		PrepareSchemaIfNecessary = true // Gerekirse tablolarý otomatik oluþtur
	})
);

// Optionally add server if you want the worker inside this app
builder.Services.AddHangfireServer();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();
