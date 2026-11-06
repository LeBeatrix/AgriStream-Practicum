
using System.ComponentModel.DataAnnotations;

namespace AgriStreamMVC.Models
{
    public class Device
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string DeviceName { get; set; } = "";

        [Required]
        [MaxLength(50)]
        public string OperationalStatus { get; set; }
            = "Idle";
    }
}
