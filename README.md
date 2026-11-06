# AgriStream IoT — Azure Farm Management System

**Module:** CLDV7112 — Cloud Development B  
**Student number:** ST10496124  
**Technology:** ASP.NET Core MVC (.NET 8), Azure Functions (.NET 8 isolated), Azure SQL Database, Azure Storage, Azure Application Insights

## Project overview

AgriStream IoT is a cloud-hosted farm monitoring prototype that displays sensor telemetry, accepts drone image uploads and manages irrigation commands. It demonstrates the use of Azure web hosting, relational and NoSQL storage, asynchronous messaging and event-driven functions.

## Azure deployment

| Service | Resource / configuration | Purpose |
| --- | --- | --- |
| Resource group | `rg-agristream` (South Africa North) | Groups project resources |
| MVC App Service | `ST-10496124` (.NET 8, Windows, Free F1) | Hosts dashboard |
| Function App | `agristream-functions-10496124` (.NET 8 isolated, Windows Consumption) | Hosts four Azure Functions |
| Storage account | `agristream10496124` (GPv2, LRS) | Blob, Table and Queue Storage |
| SQL database | `AgriStreamDB` | Users and device states |
| Application Insights | `agristream-insights` | Application and function monitoring |

**Deployed MVC website:** https://st-10496124-agfzcefgb8dzfgev.southafricanorth-01.azurewebsites.net  
**GitHub repository:** [Insert repository URL]

## Features

- Dashboard displays telemetry from the `SensorTelemetry` Azure Table, including temperature, soil moisture and humidity.
- Dashboard displays irrigation device status from Azure SQL.
- Drone image upload stores JPG/PNG files in the private `drone-images` Blob container.
- Irrigation control sends a JSON command to the `irrigation-commands` Azure Queue.
- Azure Functions expose telemetry and irrigation HTTP endpoints and process Blob/Queue events.
- Application Insights supports request and execution monitoring.

> **Implementation note:** The MVC dashboard currently accesses Azure Table Storage and Azure Queue Storage directly. The HTTP Azure Functions are separately deployed endpoints; the MVC interface is not currently wired to call those HTTP functions.

## Azure Functions

| Function | Trigger / route | Behaviour |
| --- | --- | --- |
| `GetSensorTelemetry` | HTTP GET `/api/telemetry` | Returns readings from `SensorTelemetry` |
| `QueueIrrigationCommand` | HTTP POST `/api/irrigation` | Validates `deviceId` and `action` (`ON` or `OFF`) and enqueues command |
| `ProcessIrrigationCommand` | Queue trigger: `irrigation-commands` | Updates `Devices.OperationalStatus` in Azure SQL (`ON` → `Active`; `OFF` → `Idle`) |
| `LogDroneImageUpload` | Blob trigger: `drone-images/{name}` | Records filename, size, timestamp and status in `DroneImageLogs` Table Storage |

The HTTP endpoints use **Function** authorisation in Azure. Do not publish function keys.

## Storage structure

- **Blob container:** `drone-images` — uploaded drone images.
- **Table:** `SensorTelemetry` — sensor readings for fields A, B and C.
- **Table:** `DroneImageLogs` — audit metadata for uploaded images, created by the blob-triggered function.
- **Queue:** `irrigation-commands` — asynchronous irrigation commands.
- **SQL tables:** `AppUsers`, `Devices` — application user data model and irrigation device status.

## Repository structure

```text
AgriStreamMVC/
├── AgriStreamMVC.slnx
├── AgriStreamMVC.csproj
├── Controllers/
│   └── HomeController.cs
├── Data/
│   └── AgriStreamDbContext.cs
├── Migrations/
├── Models/
│   ├── AppUser.cs
│   ├── Device.cs
│   └── DashboardViewModel.cs
├── Views/
│   └── Home/
│       └── Index.cshtml
├── Program.cs
├── appsettings.json
└── AgriStreamFunctions/
    ├── AgriStreamFunctions.csproj
    ├── GetSensoryTelemetry.cs
    ├── QueueIrrigationCommand.cs
    ├── ProcessIrrigationCommand.cs
    ├── LogDroneImageUpload.cs
    ├── Program.cs
    ├── host.json
    └── local.settings.json  # local only; ignored by Git
```

The source filename `GetSensoryTelemetry.cs` contains the deployed function named `GetSensorTelemetry`.

## Requirements

- .NET 8 SDK
- Visual Studio 2022 with ASP.NET/web and Azure development tooling, or a compatible .NET editor
- Azure subscription and permission to access the required resources
- Azure SQL connection credentials and Azure Storage connection string
- For local Azure Functions execution: Azure Functions Core Tools v4
- For local function host storage: Azurite, or configure a suitable Azure Storage connection

## Local setup

1. Clone the repository and open `AgriStreamMVC.slnx` in Visual Studio 2022.
2. Restore NuGet packages and build the MVC and Functions projects.
3. Configure the MVC project **outside source control** using user secrets or environment variables:
   - `ConnectionStrings__DefaultConnection` — Azure SQL connection string.
   - `AgriStreamStorage` — Azure Storage connection string.
4. Configure `AgriStreamFunctions/local.settings.json` locally (never commit this file):

   ```json
   {
     "IsEncrypted": false,
     "Values": {
       "AzureWebJobsStorage": "UseDevelopmentStorage=true",
       "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
       "AgriStreamStorage": "<STORAGE_CONNECTION_STRING>",
       "SqlConnectionString": "<AZURE_SQL_CONNECTION_STRING>"
     }
   }
   ```

5. Start Azurite if using `UseDevelopmentStorage=true`, and ensure the Azure SQL firewall permits your development environment.
6. Apply EF Core migrations using your configured SQL connection (only if setting up a new database):

   ```bash
   dotnet ef database update --project AgriStreamMVC.csproj
   ```

7. Run the MVC project. Run the Functions project separately using Visual Studio or Azure Functions Core Tools.

**Security:** Never commit SQL passwords, storage keys, connection strings or `local.settings.json`. Azure App Service and Function App settings should hold production configuration. The Azure Function App's `AzureWebJobsStorage` must reference real host storage in Azure; do **not** replace it with `UseDevelopmentStorage=true`.

## Deployment

1. Publish the MVC project to the existing Azure App Service `ST-10496124`.
2. Publish `AgriStreamFunctions` to the existing Azure Function App `agristream-functions-10496124`.
3. In the MVC App Service, configure `ConnectionStrings__DefaultConnection` and `AgriStreamStorage`.
4. In the Function App, configure `AgriStreamStorage` and `SqlConnectionString`, preserving the platform-managed host storage and runtime settings.
5. Verify that exactly four functions appear in the Function App and that SQL/storage access works.

## Testing and evidence

| Test | Status / evidence |
| --- | --- |
| Deployed MVC and Function App | Published successfully |
| Four functions visible in Azure | Confirmed |
| HTTP GET telemetry | Passed — three sensor readings returned |
| HTTP POST irrigation | Local test passed; deployed test still requires confirmation |
| Queue trigger updates SQL | Local test passed; deployed test still requires confirmation |
| Blob trigger creates image log | Deployed test still requires confirmation |
| MVC load test | 50 requests, concurrency 5, 50 successful, 0 failed; average 757.68 ms; p95 1,133 ms |
| Application Insights evidence | Capture monitoring screenshots |
| App Service autoscaling | Not implemented on Free F1 plan |

**Load-test scope:** The 50-request test demonstrates responsiveness under a small controlled workload, not production-scale capacity or autoscaling.

## Known limitations

- The Free F1 MVC App Service tier does **not** support autoscale rules or multi-instance scale-out. Demonstrating App Service autoscaling requires an eligible paid tier and cost approval. The appropriate practicum subscription access remained unresolved during implementation.
- The `AppUsers` data model includes roles, but this README does **not** claim that a complete login and role-based authorisation interface has been implemented.
- The dashboard alert count is currently a placeholder.
- End-to-end cloud verification of the deployed HTTP POST, queue and blob triggers should be completed and documented before final submission.

## Submission evidence checklist

Include screenshots of the resource group, live MVC dashboard, Azure SQL tables and device status, Blob/Table/Queue Storage, all four functions, HTTP tests, queue/blob trigger logs, load-test results, Application Insights and the GitHub repository. Redact all secrets and keys.

## References

- Microsoft Learn — [Azure Functions triggers and bindings](https://learn.microsoft.com/en-us/azure/azure-functions/functions-triggers-bindings)
- Microsoft Learn — [Azure Storage redundancy](https://learn.microsoft.com/en-us/azure/storage/common/storage-redundancy)
- Microsoft Learn — [Azure App Service scaling](https://learn.microsoft.com/en-us/azure/app-service/manage-scale-up)
- Microsoft Learn — [Azure Functions monitoring](https://learn.microsoft.com/en-us/azure/azure-functions/functions-monitoring)
