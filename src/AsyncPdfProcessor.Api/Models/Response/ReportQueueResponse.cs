namespace AsyncPdfProcessor.Api.Models.Response;

public record ReportQueueResponse(Guid ReferenceNo, string Status, string Message)
{
    public static ReportQueueResponse Pending(Guid referenceNo) =>
        new(referenceNo,
            "Pending",
            "Rapor oluşturma talebiniz başarıyla alındı ve arka plana atıldı.");
}
