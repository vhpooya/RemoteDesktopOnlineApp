using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RemoteDesktopOnlineApps.Models
{
    public class RemoteSession
    {
        public RemoteSession()
        {
            ChatMessages = new HashSet<ChatMessage>();
            FileTransfers = new HashSet<FileTransfer>();
            Participants = new HashSet<SessionParticipant>();
        }

        [Key]
        public int Id { get; set; }

        public int SessionId { get; set; } // شناسه یکتای جلسه (اکسترنال)

        public int UserId { get; set; } // کاربر پشتیبانی

        public string ServerIdentifier { get; set; } // شناسه سرور مشتری

        public string ServerName { get; set; } // نام سرور مشتری

        public DateTime StartTime { get; set; } = DateTime.Now;

        public DateTime? EndTime { get; set; }

        public string Status { get; set; } // Active, Disconnected, Failed

        public string ConnectionType { get; set; } // RemoteControl, FileTransfer, Chat

        public string Notes { get; set; }

        public bool IsPasswordSaved { get; set; }

        public string SavedPasswordHash { get; set; }

        // Navigation properties
      //  [ForeignKey("UserId")]
        public virtual Users User { get; set; }

        public virtual ICollection<ChatMessage> ChatMessages { get; set; }

        public virtual ICollection<FileTransfer> FileTransfers { get; set; }

        public virtual ICollection<SessionParticipant> Participants { get; set; }

        // محاسبه مدت زمان جلسه
        [NotMapped]
        public TimeSpan Duration => EndTime.HasValue ?
            EndTime.Value - StartTime : DateTime.Now - StartTime;
    }
}