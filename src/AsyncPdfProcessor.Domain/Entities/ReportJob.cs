namespace AsyncPdfProcessor.Domain.Entities;

public class ReportJob
{
	public Guid Id { get; set; } 
	public ReportStatus Status { get; set; }
	public DateTime RequestedAt { get; set; }
	public DateTime? CompletedAt { get; set; } 
	public DateTime ExchangeRateDate { get; set; }
	public string? StoragePath { get; set; }
	public string? FailureReason { get; set; }

	public ReportJob()
	{
	}

	public ReportJob(DateTime exchangeRateDate)
	{
		Id = Guid.NewGuid();
		Status = ReportStatus.Pending;
		RequestedAt = DateTime.UtcNow;
		ExchangeRateDate = exchangeRateDate;
	}
}

public enum ReportStatus
{
	Pending,        
	Processing,     
	Completed,     
	Failed          
}
