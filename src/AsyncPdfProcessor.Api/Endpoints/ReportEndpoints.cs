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
		group.MapGet("/{referenceNo}/status", GetReportStatus);

		// POST /api/reports
		group.MapPost("", QueueReport);
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
}
