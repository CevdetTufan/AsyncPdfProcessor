namespace AsyncPdfProcessor.Application.Interfaces;

public interface IPdfReportGenerator
{
	Task ExecuteAsync(Guid reportJobId);
}
