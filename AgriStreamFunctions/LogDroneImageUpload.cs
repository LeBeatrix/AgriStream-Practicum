
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AgriStreamFunctions;

public class LogDroneImageUpload
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<LogDroneImageUpload> _logger;

    public LogDroneImageUpload(
        IConfiguration configuration,
        ILogger<LogDroneImageUpload> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [Function("LogDroneImageUpload")]
    public async Task Run(
        [BlobTrigger("drone-images/{name}",
            Connection = "AgriStreamStorage")] byte[] image,
        string name)
    {
        _logger.LogInformation(
            "Drone image detected: {Name}", name);

        var connectionString =
            _configuration["AgriStreamStorage"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "AgriStreamStorage is not configured.");
        }

        var tableClient = new TableClient(
            connectionString, "DroneImageLogs");

        await tableClient.CreateIfNotExistsAsync();

        var log = new TableEntity(
            "DroneImages",
            Guid.NewGuid().ToString())
        {
            { "FileName", name },
            { "FileSizeBytes", image.LongLength },
            { "UploadedAt", DateTimeOffset.UtcNow },
            { "Status", "Uploaded" }
        };

        await tableClient.AddEntityAsync(log);

        _logger.LogInformation(
            "Drone image {Name} logged successfully.", name);
    }
}
