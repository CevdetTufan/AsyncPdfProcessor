using AsyncPdfProcessor.Domain.Models;

namespace AsyncPdfProcessor.Application.Interfaces;

public interface ICentralBankClient
{
	Task<List<ExchangeRate>> GetTodayExchangeRatesAsync();
}
