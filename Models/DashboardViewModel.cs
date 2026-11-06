
namespace AgriStreamMVC.Models
{
    public class DashboardViewModel
    {
        public int TotalSensors { get; set; }
        public int ActiveDevices { get; set; }
        public int Alerts { get; set; }

        public List<SensorReading> Readings { get; set; }
            = new List<SensorReading>();

        public List<Device> Devices { get; set; } = new();

    }

    public class SensorReading
    {
        public string SensorId { get; set; } = "";
        public string Location { get; set; } = "";
        public double Temperature { get; set; }
        public double SoilMoisture { get; set; }
        public double Humidity { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
