using AsyncPdfProcessor.Domain.Entities;

namespace AsyncPdfProcessor.Application.Interfaces;

public interface IReportService
{
	Task<Guid> QueueReportGenerationAsync(DateTime exchangeRateDate);

	Task<ReportJob?> GetReportStatusAsync(Guid referenceNo);

	Task<ReportJob?> GetReportDownloadDetailsAsync(Guid referenceNo);
}
