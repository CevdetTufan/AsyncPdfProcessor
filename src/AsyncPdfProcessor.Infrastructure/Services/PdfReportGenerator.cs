using AsyncPdfProcessor.Application.Interfaces;
using AsyncPdfProcessor.Domain.Entities;
using AsyncPdfProcessor.Domain.Models;
using AsyncPdfProcessor.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AsyncPdfProcessor.Infrastructure.Services;
internal class PdfReportGenerator(
	ICentralBankClient tcmbClient,
	IReportStorageStrategy storageStrategy,
	AppDbContext dbContext): IPdfReportGenerator
{
	private readonly ICentralBankClient _tcmbClient = tcmbClient;
	private readonly IReportStorageStrategy _storageStrategy = storageStrategy;
	private readonly AppDbContext _dbContext = dbContext;

	[AutomaticRetry(Attempts = 3)] 
	public async Task ExecuteAsync(Guid reportJobId)
	{
		var job = await _dbContext.ReportJobs.FirstOrDefaultAsync(j => j.Id == reportJobId);
		if (job == null) return;

		job.Status = ReportStatus.Processing;
		await _dbContext.SaveChangesAsync();

		try
		{
			var rates = await _tcmbClient.GetTodayExchangeRatesAsync();

			var pdfContent = GeneratePdfBytes(rates); 

			var storagePath = await _storageStrategy.SaveReportAsync(job.Id, pdfContent);

			job.StoragePath = storagePath;
			job.Status = ReportStatus.Completed;
			job.CompletedAt = DateTime.UtcNow;
		}
		catch (Exception ex)
		{
			job.Status = ReportStatus.Failed;
			job.FailureReason = $"Rapor oluşturulurken hata: {ex.Message}";
			job.CompletedAt = DateTime.UtcNow;

			throw;
		}
		finally
		{
			await _dbContext.SaveChangesAsync();
		}
	}

	private static byte[] GeneratePdfBytes(List<ExchangeRate> rates)
	{
		// Use the current UTC date as the report date. If you have a specific ExchangeRateDate,
		// pass it into this method instead of using DateTime.UtcNow.
		var reportDate = DateTime.UtcNow;

		using var ms = new MemoryStream();

		var document = Document.Create(container =>
		{
			container.Page(page =>
			{
				page.Size(PageSizes.A4);
				page.Margin(20);
				page.PageColor(Colors.White);

				page.Header().Row(row =>
				{
					row.ConstantItem(0).Column(col => { });
				});

				page.Content().Column(column =>
				{
					column.Spacing(8);

					column.Item().Text($"Rapor Tarihi: {reportDate:dd.MM.yyyy}")
						.FontSize(16)
						.Bold()
						.AlignLeft();

					column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

					column.Item().PaddingTop(6).Element(c =>
					{
						c.Table(table =>
						{
							// Define columns: Unit | Code | Name | Buying | Selling
							table.ColumnsDefinition(columns =>
							{
								columns.ConstantColumn(50);   // Unit
								columns.ConstantColumn(60);   // CurrencyCode
								columns.RelativeColumn();     // Name
								columns.ConstantColumn(80);   // Buying
								columns.ConstantColumn(80);   // Selling
							});

							// Header row
							table.Header(header =>
							{
								header.Cell().Element(CellHeader).Text("Birim");
								header.Cell().Element(CellHeader).Text("Kod");
								header.Cell().Element(CellHeader).Text("Kur");
								header.Cell().Element(CellHeader).AlignRight().Text("Alış");
								header.Cell().Element(CellHeader).AlignRight().Text("Satış");
							});

							// Data rows
							foreach (var r in rates)
							{
								table.Cell().Element(CellBody).Text(r.Unit.ToString());
								table.Cell().Element(CellBody).Text(r.CurrencyCode ?? "-");
								table.Cell().Element(CellBody).Text(r.Name ?? "-");
								table.Cell().Element(CellBody).AlignRight().Text(r.BuyingRate.ToString("N4"));
								table.Cell().Element(CellBody).AlignRight().Text(r.SellingRate.ToString("N4"));
							}
						});
					});
				});

				page.Footer().AlignCenter().Text(x =>
				{
					x.Span("Sayfa ").FontSize(9);
					x.CurrentPageNumber().FontSize(9);
					x.Span(" / ").FontSize(9);
					x.TotalPages().FontSize(9);
				});
			});
		});

		document.GeneratePdf(ms);

		return ms.ToArray();

		// Local helpers for cell styling
		static IContainer CellHeader(IContainer container) =>
			container.Padding(6).Background(Colors.Grey.Lighten3).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);

		static IContainer CellBody(IContainer container) =>
			container.Padding(6).BorderBottom(1).BorderColor(Colors.Grey.Lighten4);
	}
}
