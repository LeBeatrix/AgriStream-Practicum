
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;

namespace AgriStreamFunctions;

public class GetSensorTelemetry
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GetSensorTelemetry> _logger;

    public GetSensorTelemetry(
        IConfiguration configuration,
        ILogger<GetSensorTelemetry> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [Function("GetSensorTelemetry")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get",
            Route = "telemetry")] HttpRequestData req)
    {
        try
        {
            string? connectionString =
                _configuration["AgriStreamStorage"];

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "AgriStreamStorage is not configured.");
            }

            var tableClient = new TableClient(
                connectionString, "SensorTelemetry");

            var readings = new List<object>();

            await foreach (var entity in
                tableClient.QueryAsync<TableEntity>())
            {
                readings.Add(new
                {
                    sensorId = entity.RowKey,
                    location = entity.GetString("Location"),
                    temperature = entity.GetDouble("Temperature"),
                    soilMoisture = entity.GetDouble("SoilMoisture"),
                    humidity = entity.GetDouble("Humidity"),
                    timestamp = entity.Timestamp
                });
            }

            var response =
                req.CreateResponse(HttpStatusCode.OK);

            await response.WriteAsJsonAsync(readings);

            _logger.LogInformation(
                "Retrieved {Count} sensor readings.",
                readings.Count);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to retrieve sensor telemetry.");

            var response = req.CreateResponse(
                HttpStatusCode.InternalServerError);

            await response.WriteStringAsync(
                "Unable to retrieve sensor telemetry.");

            return response;
        }
    }
}
