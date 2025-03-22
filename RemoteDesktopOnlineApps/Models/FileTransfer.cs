using System;
using System.ComponentModel.DataAnnotations;
namespace RemoteDesktopOnlineApps.Models
{
    public class FileTransfer
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string TransferId { get; set; }

        [Required]
        public int RemoteSessionId { get; set; }

        // اضافه کردن رابطه مستقیم با کاربر
        [Required]
        public int UserId { get; set; }

        [Required]
        public string FileName { get; set; }

        public string SourcePath { get; set; }

        public string DestinationPath { get; set; }

        public long FileSize { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public bool IsUpload { get; set; }

        [Required]
        public string Status { get; set; }

        public int ProgressPercentage { get; set; }

        public string ErrorMessage { get; set; }

        public int? Chunks { get; set; }

        // رابطه با RemoteSession
        public virtual RemoteSession RemoteSession { get; set; }

        // رابطه با User
        public virtual Users User { get; set; }
    }
}