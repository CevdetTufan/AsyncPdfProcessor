using AsyncPdfProcessor.Application.Interfaces;
using AsyncPdfProcessor.Domain.Models;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Xml.Linq;

namespace AsyncPdfProcessor.Infrastructure.Clients;

internal class CentralBankClient(HttpClient httpClient, IConfiguration configuration) : ICentralBankClient
{
	private readonly HttpClient _httpClient = httpClient;

	private readonly string _tcmbApiUrl = 
		configuration["CentralBank:ApiUrl"] ?? 
		throw new ArgumentNullException(nameof(configuration), "CentralBank:ApiUrl configuration is missing.");

	private static readonly CultureInfo trCulture = new("tr-TR");

	public async Task<List<ExchangeRate>> GetTodayExchangeRatesAsync()
	{
		string xmlString;
		try
		{
			xmlString = await _httpClient.GetStringAsync(_tcmbApiUrl);
		}
		catch (HttpRequestException ex)
		{
			//Hangfire retry again
			throw new InvalidOperationException("Data could not be retrieved from the TCMB API. The operation will be retried.", ex);
		}

		var doc = XDocument.Parse(xmlString);
		var rates = new List<ExchangeRate>();

		if (doc.Root == null) return rates;

		var currencyElements = doc.Root.Elements("Currency");

		foreach (var el in currencyElements)
		{
			var currencyCode = el.Attribute("CurrencyCode")?.Value;
			var unitText = el.Element("Unit")?.Value?.Trim();
			var buyText = el.Element("ForexBuying")?.Value?.Trim();
			var sellText = el.Element("ForexSelling")?.Value?.Trim();

			if (!int.TryParse(unitText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unit))
				unit = 1;

			var buying = ParseDecimalNormalized(buyText, trCulture, 0m);
			var selling = ParseDecimalNormalized(sellText, trCulture, 0m);

			rates.Add(new ExchangeRate
			{
				CurrencyCode = currencyCode,
				Unit = unit,
				Name = el.Element("Isim")?.Value,
				BuyingRate = buying,
				SellingRate = selling
			});
		}

		return rates;
	}

	private static decimal ParseDecimalNormalized(string? text, CultureInfo culture, decimal defaultValue)
	{
		if (string.IsNullOrWhiteSpace(text))
			return defaultValue;

		var s = text.Trim();

		if (culture.NumberFormat.NumberDecimalSeparator == "," && s.Contains('.') && !s.Contains(','))
		{
			s = s.Replace('.', ',');
		}

		if (decimal.TryParse(s, NumberStyles.Number, culture, out var result))
			return result;

		if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out result))
			return result;

		return defaultValue;
	}
}
