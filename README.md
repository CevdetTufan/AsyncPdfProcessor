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
