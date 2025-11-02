using AsyncPdfProcessor.Domain.Entities;

namespace AsyncPdfProcessor.Api.Models;

public record ReportStatusResponse(
		Guid ReferenceNo,
		ReportStatus Status,
		DateTime RequestedAt,
		DateTime? CompletedAt,
		string? FailureReason,
		string? StatusDetail 
	);


//Mapping extension method
public static class ReportStatusResponseExtensions
{
	public static ReportStatusResponse ToResponseModel(this ReportJob job)
	{
		string? statusDetail = job.Status switch
		{
			ReportStatus.Pending => "Rapor kuyruğunda bekliyor.",
			ReportStatus.Processing => "Rapor oluşturuluyor.",
			ReportStatus.Completed => "Rapor başarıyla oluşturuldu ve indirilebilir.",
			ReportStatus.Failed => $"Rapor oluşturma işlemi başarısız oldu: {job.FailureReason}",
			_ => null
		};
		return new ReportStatusResponse(
			ReferenceNo: job.Id,
			Status: job.Status,
			RequestedAt: job.RequestedAt,
			CompletedAt: job.CompletedAt,
			FailureReason: job.FailureReason,
			StatusDetail: statusDetail
		);
	}
}
