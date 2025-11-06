# AsyncPdfProcessor

Technical README - Project status, architecture and how to run

## Overview

AsyncPdfProcessor is a .NET9 application that generates PDF reports of Turkish Central Bank exchange rates asynchronously. The system queues requests, fetches exchange rates from the TCMB XML feed, renders a tabular PDF report and stores it on the local file system using the implemented storage strategy.

Implemented features (current repository state):
- HTTP API endpoints to queue and download reports
 - `POST /api/reports` : queues a report generation for specific `ExchangeRateDate`
 - `GET /api/reports/{referenceNo}/status` : checks job status
 - `GET /api/reports/{referenceNo}/download` : downloads completed PDF (`application/pdf`)
- Background job processing using Hangfire with SQL Server storage
- TCMB client that parses the official XML feed and normalizes numeric values
- PDF generation using QuestPDF producing a tabular report with columns: `Unit`, `Code`, `Name`, `Buying`, `Selling`
- EF Core `AppDbContext` with existing migrations under `src/AsyncPdfProcessor.Infrastructure/Migrations`
- Implemented `IReportStorageStrategy` and concrete local storage: `LocalFileStorageStrategy`

## Repository structure

- `src/`
 - `AsyncPdfProcessor.Api/` -> Web API project exposing HTTP endpoints and wiring infrastructure
 - `Program.cs` - app startup and DI wiring
 - `Endpoints/ReportEndpoints.cs` - API route definitions
 - `Models/Request/ReportRequest.cs` and `Models/Response/*` - DTOs
 - `AsyncPdfProcessor.Infrastructure/` -> Implementation of clients, services and persistence
 - `Clients/CentralBankClient.cs` - TCMB XML client
 - `Services/PdfReportGenerator.cs` - Hangfire job handler and PDF generation (QuestPDF)
 - `Storages/LocalFileStorageStrategy.cs` - local disk storage implementation of `IReportStorageStrategy`
 - `Persistence/AppDbContext.cs` - EF Core DbContext and migrations
 - `AsyncPdfProcessor.Application/` -> Application layer interfaces and service contracts
 - `AsyncPdfProcessor.Domain/` -> Domain models (ExchangeRate, ReportJob entity, enums)
- `tests/`
 - `AsyncPdfProcessor.Tests/` -> Integration / unit tests

## Background processing (Hangfire)

This project uses Hangfire to run report generation jobs in the background. The relevant implementation details:

- **Storage**
 - Hangfire is configured to use SQL Server storage in `Program.cs` via `UseSqlServerStorage(...)` with a custom schema name `HangFire`.
 - Connection string is read from configuration key `ConnectionStrings:DefaultConnection`.

- **Enqueue flow**
 - The API layer calls `IReportService.QueueReportGenerationAsync(exchangeRateDate)` when a report request is received.
 - `ReportService.QueueReportGenerationAsync` creates a `ReportJob` entity, persists it to the database (`ReportJobs` table) and then enqueues a Hangfire background job using `IBackgroundJobClient.Enqueue<T>`.
 - The enqueued method signature is `IPdfReportGenerator.ExecuteAsync(Guid reportJobId)`; Hangfire resolves `IPdfReportGenerator` from DI when executing the job.

- **Job execution lifecycle**
 - `PdfReportGenerator.ExecuteAsync` is the background worker entry point. Typical steps inside the job:
1. Load the `ReportJob` record by id from the DB.
2. Update job status to `Processing` and save.
3. Fetch exchange rates from the TCMB client.
4. Generate PDF bytes (QuestPDF) and persist using the configured `IReportStorageStrategy` (currently `LocalFileStorageStrategy`).
5. Update `ReportJob.StoragePath`, set status to `Completed`, set `CompletedAt` and save.
6. On exception, set `ReportJob.Status = Failed`, store `FailureReason` and rethrow to let Hangfire record the failure.

- **Retry behavior**
 - `PdfReportGenerator` has an `AutomaticRetry` attribute (configured with Attempts =3) — this instructs Hangfire to retry failed executions up to the configured number of attempts.
 - Transient errors from the TCMB client (HTTP failures) are surfaced as exceptions so Hangfire retries the job according to the retry policy.

- **Worker hosting**
 - The project registers Hangfire server via `builder.Services.AddHangfireServer()` allowing the same web application to process jobs inline (single-process worker). For production, you can run a separate dedicated worker process by hosting `AddHangfireServer` in a separate worker app or service (not required by current repository state).

- **Observability**
 - Hangfire stores job state and history in the configured SQL database. You can inspect job state using Hangfire dashboard if added; the dashboard is not currently hooked up in the default code but can be enabled easily by registering `app.UseHangfireDashboard()` in `Program.cs` and protecting it as needed.

## Components (concise)

- API: Minimal API using .NET9 `WebApplication` and OpenAPI; routes under `/api/reports`.
- Background Worker: Hangfire for queued, retriable background jobs stored in SQL Server.
- Data Access: EF Core `AppDbContext` with migrations present in the repository.
- External integration: TCMB public XML feed (`CentralBank:ApiUrl` in configuration).
- PDF Generation: QuestPDF producing A4 tabular reports.
- Storage: `LocalFileStorageStrategy` persists generated PDFs to disk.

## Storage (concrete implementation)

The project contains a concrete implementation `LocalFileStorageStrategy` at `src/AsyncPdfProcessor.Infrastructure/Storages/LocalFileStorageStrategy.cs` which implements `IReportStorageStrategy`.

Behavior:
- Storage directory: `${ContentRootPath}/LocalReports` (the API application's content root path + `LocalReports`).
- File naming: `{reportId}.pdf` (GUID-based file name).
- API download: `GET /api/reports/{referenceNo}/download` streams the file found at the stored path with content type `application/pdf`.

## Database migrations

EF Core migrations are included in the repository under `src/AsyncPdfProcessor.Infrastructure/Migrations`. Apply migrations using `dotnet ef` with the infrastructure project as the `--project` and the API as the `--startup-project`.

Example commands (run from repository root):

```bash
# install tooling if needed
dotnet tool install --global dotnet-ef

# apply migrations
dotnet ef database update \
 --project src/AsyncPdfProcessor.Infrastructure \
 --startup-project src/AsyncPdfProcessor.Api
```

Notes:
- `--project` targets the project containing the `DbContext` (Infrastructure)
- `--startup-project` targets the runnable project used for configuration and resolving connection strings (Api)

## How to run (development)

Prerequisites:
- .NET9 SDK
- SQL Server instance and a connection string configured as `ConnectionStrings:DefaultConnection` in `src/AsyncPdfProcessor.Api/appsettings.*.json`

Steps:
1. `dotnet restore`
2. `dotnet build`
3. Apply migrations (see Database migrations section)
4. `dotnet run --project src/AsyncPdfProcessor.Api`

The API will be available on the configured `applicationUrl` (see `src/AsyncPdfProcessor.Api/Properties/launchSettings.json`).

## Configuration keys used by the project

- `CentralBank:ApiUrl` - URL to TCMB XML feed
- `ConnectionStrings:DefaultConnection` - SQL Server connection string used by Hangfire and EF Core

## Example API flow (concise)

1. `POST /api/reports` with `{ "ExchangeRateDate": "2025-11-06" }` returns a `referenceNo` (Guid).
2. Background job generates PDF and saves it using `LocalFileStorageStrategy`.
3. `GET /api/reports/{referenceNo}/download` returns the PDF stream with `application/pdf`.

## Example screenshot

![PDF download screenshot](assets/pdf-download-screenshot.png)
