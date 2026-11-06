
using System.ComponentModel.DataAnnotations;

namespace AgriStreamMVC.Models
{
    public class AppUser
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = "";

        [Required]
        public string PasswordHash { get; set; } = "";

        [Required]
        [MaxLength(30)]
        public string Role { get; set; } = "Farmer";
    }
}
