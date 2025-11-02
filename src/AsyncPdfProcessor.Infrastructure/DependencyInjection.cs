using AsyncPdfProcessor.Application.Interfaces;
using AsyncPdfProcessor.Infrastructure.Clients;
using AsyncPdfProcessor.Infrastructure.Services;
using AsyncPdfProcessor.Infrastructure.Storages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AsyncPdfProcessor.Infrastructure;

public static class DependencyInjection
{
	public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddDbContext<Persistence.AppDbContext>(options =>
		{
			var connectionString = configuration.GetConnectionString("DefaultConnection");
			options.UseSqlServer(connectionString);
		});

		services.AddHttpClient<ICentralBankClient, CentralBankClient>();
		services.AddScoped<IReportService, ReportService>();
		services.AddScoped<IReportStorageStrategy, LocalFileStorageStrategy>();
		services.AddScoped<IPdfReportGenerator, PdfReportGenerator>();

		return services;
	}
}
