using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RemoteDesktopOnlineApps.Models
{
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        public int RemoteSessionId { get; set; }

        public int SenderId { get; set; }

        public string SenderName { get; set; }

        public string Message { get; set; }

        public DateTime Timestamp { get; set; }

        public bool IsFromSupport { get; set; }

        // Navigation property to Users
        [ForeignKey("SenderId")]
        public virtual Users Sender { get; set; }

        // Navigation property to RemoteSession
        [ForeignKey("RemoteSessionId")]
        public virtual RemoteSession RemoteSession { get; set; }
    }
}