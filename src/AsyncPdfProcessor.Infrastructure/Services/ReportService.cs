using AsyncPdfProcessor.Application.Interfaces;
using AsyncPdfProcessor.Domain.Entities;
using AsyncPdfProcessor.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace AsyncPdfProcessor.Infrastructure.Services;

internal class ReportService(AppDbContext dbContext, IBackgroundJobClient jobClient) : IReportService
{
	private readonly AppDbContext _dbContext = dbContext;
	private readonly IBackgroundJobClient _jobClient = jobClient;

	public async Task<Guid> QueueReportGenerationAsync(DateTime exchangeRateDate)
	{
		var newJob = new ReportJob(exchangeRateDate);

		_dbContext.ReportJobs.Add(newJob);
		await _dbContext.SaveChangesAsync();

		_jobClient.Enqueue<IPdfReportGenerator>(generator =>generator.ExecuteAsync(newJob.Id));

		return newJob.Id;
	}

	public async Task<ReportJob?> GetReportStatusAsync(Guid referenceNo)
	{
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
