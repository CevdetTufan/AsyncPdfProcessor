using AsyncPdfProcessor.Api.Models;
using AsyncPdfProcessor.Application.Interfaces;
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
}
