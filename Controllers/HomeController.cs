
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AgriStreamMVC.Models;

namespace AgriStreamMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            var model = new DashboardViewModel
            {
                TotalSensors = 3,
                ActiveDevices = 2,
                Alerts = 1,

                Readings = new List<SensorReading>
                {
                    new SensorReading
                    {
                        SensorId = "SEN-001",
                        Location = "Field A",
                        Temperature = 28.5,
                        SoilMoisture = 42,
                        Humidity = 65,
                        Timestamp = DateTime.UtcNow
                    },
                    new SensorReading
                    {
                        SensorId = "SEN-002",
                        Location = "Field B",
                        Temperature = 31.2,
                        SoilMoisture = 28,
                        Humidity = 58,
                        Timestamp = DateTime.UtcNow
                    },
                    new SensorReading
                    {
                        SensorId = "SEN-003",
                        Location = "Field C",
                        Temperature = 26.8,
                        SoilMoisture = 55,
                        Humidity = 72,
                        Timestamp = DateTime.UtcNow
                    }
                }
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TriggerIrrigation()
        {
            TempData["Message"] =
                "Demo only: irrigation request received.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UploadDroneImage(
            IFormFile? image)
        {
            if (image == null || image.Length == 0)
            {
                TempData["Message"] =
                    "Please select an image.";
            }
            else
            {
                TempData["Message"] =
                    "Demo only: image selected. " +
                    "Azure upload is not connected yet.";
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
                RequestId =
                    Activity.Current?.Id ??
                    HttpContext.TraceIdentifier
            });
        }
    }
}
