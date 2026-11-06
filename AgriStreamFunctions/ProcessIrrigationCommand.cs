
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AgriStreamFunctions;

public class ProcessIrrigationCommand
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProcessIrrigationCommand> _logger;

    public ProcessIrrigationCommand(
        IConfiguration configuration,
        ILogger<ProcessIrrigationCommand> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [Function("ProcessIrrigationCommand")]
    public async Task Run(
        [QueueTrigger("irrigation-commands",
            Connection = "AgriStreamStorage")] string message)
    {
        _logger.LogInformation(
            "Processing irrigation command.");

        var command = JsonSerializer.Deserialize<IrrigationMessage>(
            message,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (command == null ||
            command.DeviceId <= 0 ||
            (command.Action != "ON" && command.Action != "OFF"))
        {
            throw new InvalidOperationException(
                "Invalid irrigation command.");
        }

        var connectionString =
            _configuration["SqlConnectionString"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "SqlConnectionString is not configured.");
        }

        var newStatus = command.Action == "ON"
            ? "Active"
            : "Idle";

        await using var connection =
            new SqlConnection(connectionString);

        await connection.OpenAsync();

        const string sql = """
            UPDATE Devices
            SET OperationalStatus = @Status
            WHERE Id = @DeviceId
            """;

        await using var sqlCommand =
            new SqlCommand(sql, connection);

        sqlCommand.Parameters.AddWithValue(
            "@Status", newStatus);

        sqlCommand.Parameters.AddWithValue(
            "@DeviceId", command.DeviceId);

        var rowsAffected =
            await sqlCommand.ExecuteNonQueryAsync();

        if (rowsAffected == 0)
        {
            throw new InvalidOperationException(
                $"Device {command.DeviceId} was not found.");
        }

        _logger.LogInformation(
            "Device {DeviceId} updated to {Status}.",
            command.DeviceId, newStatus);
    }

    private class IrrigationMessage
    {
        public int DeviceId { get; set; }
        public string? Action { get; set; }
    }
}
