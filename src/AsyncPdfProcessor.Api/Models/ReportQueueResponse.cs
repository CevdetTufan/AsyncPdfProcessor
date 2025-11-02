namespace AsyncPdfProcessor.Api.Models;

public record ReportQueueResponse(
		Guid ReferenceNo,
		string Status,
		string Message
	);
