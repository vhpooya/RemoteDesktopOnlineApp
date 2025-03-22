using System;
using System.ComponentModel.DataAnnotations;

namespace RemoteDesktopOnlineApps.Models
{
    public class ClientConnectionInfo
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ClientIdentifier { get; set; }  // Changed to match with ClientRegistration

        public string SecretKey { get; set; }

        public string ConnectionPassword { get; set; }

        public string MachineName { get; set; }

        public string ServerUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? LastUpdatedAt { get; set; }
    }
}