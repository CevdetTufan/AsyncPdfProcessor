using AsyncPdfProcessor.Application.Interfaces;
using Microsoft.Extensions.Hosting;

namespace AsyncPdfProcessor.Infrastructure.Storages;

internal class LocalFileStorageStrategy: IReportStorageStrategy
{
	private readonly string _storageDirectory;
	private const string ReportsFolderName = "LocalReports"; 

	public LocalFileStorageStrategy(IHostEnvironment environment)
	{
		_storageDirectory = Path.Combine(environment.ContentRootPath, ReportsFolderName);

		if (!Directory.Exists(_storageDirectory))
		{
			Directory.CreateDirectory(_storageDirectory);
		}
	}

	public async Task<string> SaveReportAsync(Guid referenceId, byte[] fileContent)
	{
		var fileName = $"{referenceId}.pdf";
		var fullPath = Path.Combine(_storageDirectory, fileName);

		await File.WriteAllBytesAsync(fullPath, fileContent);

		return fullPath;
	}

	public Task<Stream> GetReportStreamAsync(string storagePath)
	{
		if (!File.Exists(storagePath))
		{
			throw new FileNotFoundException("Yerel diskte rapor dosyası bulunamadı.", storagePath);
		}

		var stream = new FileStream(storagePath, FileMode.Open, FileAccess.Read);
		return Task.FromResult<Stream>(stream);
	}

	public Task DeleteReportAsync(string storagePath)
	{
		if (File.Exists(storagePath))
		{
			File.Delete(storagePath);
		}
		return Task.CompletedTask;
	}
}
