namespace AsyncPdfProcessor.Domain.Models;

public class ExchangeRate
{
	public string? CurrencyCode { get; set; } // Örn: USD, EUR
	public int Unit { get; set; }              // Örn: 1
	public string? Name { get; set; }          // Örn: ABD DOLARI
	public decimal BuyingRate { get; set; }   // Efektif Alış
	public decimal SellingRate { get; set; }  // Efektif Satış

	public override string ToString()
	{
		return $"{Unit} {CurrencyCode} ({Name}): Alış={BuyingRate}, Satış={SellingRate}";
	}
}
