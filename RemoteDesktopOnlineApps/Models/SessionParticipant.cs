using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RemoteDesktopOnlineApps.Models
{
    public class SessionParticipant
    {
        [Key]
        public int Id { get; set; }

        public int RemoteSessionId { get; set; }

        public int UserId { get; set; }

        public string UserName { get; set; }

        public DateTime JoinTime { get; set; } = DateTime.Now;

        public DateTime? LeaveTime { get; set; }

        [NotMapped]
        public bool IsActive => !LeaveTime.HasValue;

        // Navigation properties
       // [ForeignKey("RemoteSessionId")]
        public virtual RemoteSession RemoteSession { get; set; }

        //[ForeignKey("UserId")]
        public virtual Users User { get; set; }
    }
}