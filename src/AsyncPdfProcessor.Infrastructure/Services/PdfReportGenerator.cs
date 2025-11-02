using AsyncPdfProcessor.Application.Interfaces;
using AsyncPdfProcessor.Domain.Entities;
using AsyncPdfProcessor.Domain.Models;
using AsyncPdfProcessor.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;

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
		return new byte[1024];
	}
}
