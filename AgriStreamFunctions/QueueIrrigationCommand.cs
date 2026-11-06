
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace AgriStreamFunctions;

public class QueueIrrigationCommand
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<QueueIrrigationCommand> _logger;

    public QueueIrrigationCommand(
        IConfiguration configuration,
        ILogger<QueueIrrigationCommand> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [Function("QueueIrrigationCommand")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post",
            Route = "irrigation")] HttpRequestData req)
    {
        IrrigationCommand? command;

        try
        {
            command = await JsonSerializer.DeserializeAsync<IrrigationCommand>(
                req.Body,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }
        catch (JsonException)
        {
            return await BadRequest(req, "Invalid JSON request.");
        }

        if (command is null ||
            command.DeviceId <= 0 ||
            string.IsNullOrWhiteSpace(command.Action) ||
            !new[] { "ON", "OFF" }.Contains(
                command.Action.Trim().ToUpperInvariant()))
        {
            return await BadRequest(
                req, "Provide a valid DeviceId and Action (ON or OFF).");
        }

        try
        {
            var connectionString =
                _configuration["AgriStreamStorage"];

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    "AgriStreamStorage is not configured.");

            var queueClient = new QueueClient(
                connectionString,
                "irrigation-commands");

            var message = JsonSerializer.Serialize(new
            {
                deviceId = command.DeviceId,
                action = command.Action.Trim().ToUpperInvariant(),
                requestedAt = DateTimeOffset.UtcNow
            });

            await queueClient.SendMessageAsync(message);

            _logger.LogInformation(
                "Queued irrigation command for device {DeviceId}.",
                command.DeviceId);

            var response = req.CreateResponse(HttpStatusCode.Accepted);
            await response.WriteAsJsonAsync(new
            {
                message = "Irrigation command queued successfully.",
                deviceId = command.DeviceId
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to queue irrigation command.");

            var response = req.CreateResponse(
                HttpStatusCode.InternalServerError);

            await response.WriteStringAsync(
                "Unable to queue irrigation command.");

            return response;
        }
    }

    private static async Task<HttpResponseData> BadRequest(
        HttpRequestData req, string message)
    {
        var response = req.CreateResponse(HttpStatusCode.BadRequest);
        await response.WriteAsJsonAsync(new { error = message });
        return response;
    }

    public class IrrigationCommand
    {
        public int DeviceId { get; set; }
        public string? Action { get; set; }
    }
}
