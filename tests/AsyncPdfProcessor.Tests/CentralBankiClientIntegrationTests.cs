using AsyncPdfProcessor.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using AsyncPdfProcessor.Infrastructure.Clients;

namespace AsyncPdfProcessor.Tests;

public class CentralBankiClientIntegrationTests
{
	private const string TcmbUrlKey = "CentralBank:ApiUrl";
	private readonly ICentralBankClient _client;

	public CentralBankiClientIntegrationTests()
	{
		var inMemorySettings = new Dictionary<string, string?>
		{
            // App.config/appsettings'te okunan değeri buraya tanımlıyoruz
            { TcmbUrlKey, "https://www.tcmb.gov.tr/kurlar/today.xml" }
		};

		IConfiguration configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(inMemorySettings!)
			.Build();

		var httpClient = new HttpClient();

		_client = new CentralBankClient(httpClient, configuration);
	}

	[Fact]
	public async Task GetTodayExchangeRatesAsync_ShouldFetchDataFromConfiguredUrlAndReturnValidRates()
	{
		// Arrange & Act
		var rates = await _client.GetTodayExchangeRatesAsync();

		// Assert
		Assert.NotNull(rates);
		Assert.True(rates.Count > 0, "Konfigüre edilen URL'den döviz kurları çekilmelidir.");

		var usdRate = rates.FirstOrDefault(r => r.CurrencyCode == "USD");
		Assert.NotNull(usdRate);
	}
}
