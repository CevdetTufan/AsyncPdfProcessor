using AsyncPdfProcessor.Application.Interfaces;
using AsyncPdfProcessor.Infrastructure.Clients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AsyncPdfProcessor.Infrastructure;

public static class DependencyInjection
{
	public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddHttpClient<ICentralBankClient, CentralBankClient>();

		return services;
	}
}
