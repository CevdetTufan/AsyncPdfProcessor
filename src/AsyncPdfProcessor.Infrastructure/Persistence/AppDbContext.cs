using AsyncPdfProcessor.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AsyncPdfProcessor.Infrastructure.Persistence;

public class AppDbContext: DbContext
{
	public DbSet<ReportJob> ReportJobs { get; set; }

	public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
	{
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<ReportJob>()
			.Property(j => j.Status)
			.HasConversion<string>()
			.HasMaxLength(20); 

		base.OnModelCreating(modelBuilder);
	}
}
