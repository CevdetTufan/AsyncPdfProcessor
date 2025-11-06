using AsyncPdfProcessor.Api.Models.Request;
using AsyncPdfProcessor.Api.Models.Response;
using AsyncPdfProcessor.Application.Interfaces;
using AsyncPdfProcessor.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace AsyncPdfProcessor.Api.Endpoints;

public static class ReportEndpoints
{
	public static void MapReportEndpoints(this IEndpointRouteBuilder app)
	{
		var group = app.MapGroup("/api/reports")
							   .WithTags("Rapor Yönetimi")
							   .WithOpenApi();

		// GET /api/reports/{referenceNo}/status
		group.MapGet("/{referenceNo}/status", GetReportStatus).WithDisplayName("status");

		// POST /api/reports
		group.MapPost("", QueueReport).WithDisplayName("queue");

		// GET /api/reports/{referenceNo}/download
		group.MapGet("/{referenceNo}/download", DownloadReport).WithDisplayName("download");
	}

	private static async Task<IResult> GetReportStatus(
			[FromRoute] Guid referenceNo,
			IReportService reportService)
	{
		var job = await reportService.GetReportStatusAsync(referenceNo);

		if (job == null)
		{
			return Results.NotFound(new { Message = $"Referans numarasına ({referenceNo}) ait iş bulunamadı." });
		}

		return Results.Ok(job.ToResponseModel());
	}

	private static async Task<IResult> QueueReport([FromBody] ReportRequest request, IReportService reportService)
	{
		if (request.ExchangeRateDate.Date > DateTime.Today)
		{
			return Results.BadRequest(new { Message = "İstenen kur tarihi bugünden ileri olamaz." });
		}

		var referenceNo = await reportService.QueueReportGenerationAsync(request.ExchangeRateDate);

		return Results.Accepted(
			$"/api/reports/{referenceNo}/status",
			ReportQueueResponse.Pending(referenceNo)
		);
	}

	private static async Task<IResult> DownloadReport(
		[FromRoute] Guid referenceNo,
		IReportService reportService,
		IReportStorageStrategy storageStrategy)
	{
		var job = await reportService.GetReportDownloadDetailsAsync(referenceNo);

		if (job == null)
		{
			return Results.NotFound(new { Message = "Rapor bulunamadı veya henüz kuyrukta/işleniyor. Lütfen durumu sorgulayın." });
		}

		if (job.Status == ReportStatus.Failed)
		{
			return Results.Conflict(new { Message = $"İşlem başarısız oldu ve indirme yapılamıyor. Hata: {job.FailureReason}" });
		}

		if (string.IsNullOrEmpty(job.StoragePath))
		{
			return Results.Json(
				new { Message = "Rapor tamamlanmış görünüyor ancak dosya yolu kaydedilmemiş." },
				statusCode: 500
			);
		}

		Stream fileStream;
		try
		{
			fileStream = await storageStrategy.GetReportStreamAsync(job.StoragePath);
		}
		catch (FileNotFoundException)
		{
			return Results.NotFound(new { Message = "Dosya depolama alanında bulunamadı (Sunucu hatası)." });
		}

		return Results.File(
			fileStream,
			contentType: "application/pdf",
			fileDownloadName: $"TCMB_Rapor_{job.ExchangeRateDate:yyyyMMdd}_{referenceNo}.pdf"
		);
	}
}
