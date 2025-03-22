using System;
using System.ComponentModel.DataAnnotations;

namespace RemoteDesktopOnlineApps.Models
{
    public class ClientRegistration
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ClientIdentifier { get; set; }

        [Required]
        public string Password { get; set; }

        public string MachineName { get; set; }

        public string OperatingSystem { get; set; }

        public DateTime RegisteredDate { get; set; } = DateTime.Now;

        public DateTime? LastUpdated { get; set; }

        public bool IsActive { get; set; } = true;
    }
}