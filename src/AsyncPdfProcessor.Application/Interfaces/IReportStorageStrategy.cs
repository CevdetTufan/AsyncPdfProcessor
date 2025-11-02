namespace AsyncPdfProcessor.Application.Interfaces;

public interface IReportStorageStrategy
{
	Task<string> SaveReportAsync(Guid referenceId, byte[] fileContent);
	Task<Stream> GetReportStreamAsync(string storagePath);
	Task DeleteReportAsync(string storagePath);
}
