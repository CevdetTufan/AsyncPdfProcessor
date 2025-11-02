using AsyncPdfProcessor.Application.Interfaces;
using AsyncPdfProcessor.Domain.Entities;
using AsyncPdfProcessor.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncPdfProcessor.Infrastructure.Services;

internal class ReportService : IReportService
{
	private readonly AppDbContext _dbContext;
	private readonly IBackgroundJobClient _jobClient;

	public ReportService(AppDbContext dbContext,IBackgroundJobClient jobClient)
	{
		_dbContext = dbContext;
		_jobClient = jobClient;
	}

	public async Task<Guid> QueueReportGenerationAsync(DateTime exchangeRateDate)
	{
		// 1. Yeni iş kaydını oluştur (Status = Pending)
		var newJob = new ReportJob(exchangeRateDate);

		// 2. DB'ye kaydet
		_dbContext.ReportJobs.Add(newJob);
		await _dbContext.SaveChangesAsync();

		// 3. Hangfire'ı tetikle!
		// Enqueue<T>(Action<T> methodCall) kullanılır. 
		// Burada IPdfReportGenerator'ın somut implementasyonu (Worker) çağrılır.
		//_jobClient.Enqueue<IPdfReportGenerator>(generator =>
			//generator.ExecuteAsync(newJob.Id));

		// 4. Referans numarasını (Job ID) kullanıcıya dön
		return newJob.Id;
	}

	public async Task<ReportJob?> GetReportStatusAsync(Guid referenceNo)
	{
		// Sadece durum sorguladığı için AsNoTracking kullanmak performansı artırır.
		return await _dbContext.ReportJobs
			.AsNoTracking()
			.FirstOrDefaultAsync(j => j.Id == referenceNo);
	}

	public async Task<ReportJob?> GetReportDownloadDetailsAsync(Guid referenceNo)
	{
		return await _dbContext.ReportJobs
			.AsNoTracking()
			.Where(j => j.Id == referenceNo && (j.Status == ReportStatus.Completed || j.Status == ReportStatus.Failed))
			.FirstOrDefaultAsync();
	}
}
