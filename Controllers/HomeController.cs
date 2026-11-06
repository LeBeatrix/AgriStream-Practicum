
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using AgriStreamMVC.Data;
using AgriStreamMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text.Json;

namespace AgriStreamMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AgriStreamDbContext _context;
        private readonly TableClient _tableClient;
        private readonly IConfiguration _configuration;

        public HomeController(
            ILogger<HomeController> logger,
            AgriStreamDbContext context,
            TableClient tableClient,
            IConfiguration configuration)
        {
            _logger = logger;
            _context = context;
            _tableClient = tableClient;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            var model = new DashboardViewModel
            {
                Alerts = 0
            };

            var readings = new List<SensorReading>();

            await foreach (
                var entity in _tableClient.QueryAsync<TableEntity>())
            {
                readings.Add(new SensorReading
                {
                    SensorId = entity.RowKey,
                    Location = entity.GetString("Location") ?? "Unknown",
                    Temperature = entity.GetDouble("Temperature") ?? 0,
                    SoilMoisture = entity.GetDouble("SoilMoisture") ?? 0,
                    Humidity = entity.GetDouble("Humidity") ?? 0,
                    Timestamp = entity.Timestamp?.UtcDateTime
                        ?? DateTime.UtcNow
                });
            }

            model.Readings = readings;
            model.TotalSensors = readings.Count;

            model.Devices = await _context.Devices
                .AsNoTracking()
                .ToListAsync();

            model.ActiveDevices = model.Devices.Count(
                d => d.OperationalStatus == "Active");

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TriggerIrrigation()
        {
            try
            {
                var connectionString =
                    _configuration["AgriStreamStorage"];

                if (string.IsNullOrWhiteSpace(connectionString))
                    throw new InvalidOperationException(
                        "Azure Storage is not configured.");

                var device = await _context.Devices
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (device == null)
                {
                    TempData["Message"] = "No irrigation device found.";
                    return RedirectToAction(nameof(Index));
                }

                var queueClient = new QueueClient(
                    connectionString,
                    "irrigation-commands");

                var message = JsonSerializer.Serialize(new
                {
                    deviceId = device.Id,
                    action = "ON",
                    requestedAt = DateTimeOffset.UtcNow
                });

                await queueClient.SendMessageAsync(message);

                TempData["Message"] =
                    "Irrigation command queued successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex, "Failed to queue irrigation command.");

                TempData["Message"] =
                    "Unable to queue irrigation command.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDroneImage(
            IFormFile? image)
        {
            if (image == null || image.Length == 0)
            {
                TempData["Message"] = "Please select an image.";
                return RedirectToAction(nameof(Index));
            }

            const long maxSize = 10 * 1024 * 1024;

            var extension = Path.GetExtension(image.FileName)
                .ToLowerInvariant();

            var allowedExtensions = new[]
            {
                ".jpg", ".jpeg", ".png"
            };

            if (image.Length > maxSize ||
                !allowedExtensions.Contains(extension))
            {
                TempData["Message"] =
                    "Only JPG or PNG images up to 10 MB are allowed.";

                return RedirectToAction(nameof(Index));
            }

            try
            {
                var connectionString =
                    _configuration["AgriStreamStorage"];

                if (string.IsNullOrWhiteSpace(connectionString))
                    throw new InvalidOperationException(
                        "Azure Storage is not configured.");

                var blobServiceClient =
                    new BlobServiceClient(connectionString);

                var containerClient =
                    blobServiceClient.GetBlobContainerClient(
                        "drone-images");

                var fileName =
                    $"{Guid.NewGuid():N}{extension}";

                var blobClient =
                    containerClient.GetBlobClient(fileName);

                await using var stream = image.OpenReadStream();

                await blobClient.UploadAsync(stream);

                TempData["Message"] =
                    "Drone image uploaded successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex, "Drone image upload failed.");

                TempData["Message"] =
                    "Unable to upload drone image.";
            }

            return RedirectToAction(nameof(Index));
        }

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ??
                    HttpContext.TraceIdentifier
            });
        }
    }
}
