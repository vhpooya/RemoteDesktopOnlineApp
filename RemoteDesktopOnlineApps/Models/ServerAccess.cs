using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RemoteDesktopOnlineApps.Models
{
    /// <summary>
    /// مدل دسترسی به سرور
    /// </summary>
    public class ServerAccess
    {
        /// <summary>
        /// شناسه
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// شناسه کاربر
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// شناسه سرور
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string ServerIdentifier { get; set; }

        /// <summary>
        /// اجازه دسترسی غیرهمزمان
        /// </summary>
        public bool AllowUnattendedAccess { get; set; }

        /// <summary>
        /// زمان ایجاد دسترسی
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// آخرین زمان به‌روزرسانی
        /// </summary>
        public DateTime? LastUpdatedAt { get; set; }

        /// <summary>
        /// رابطه با کاربر
        /// </summary>
       // [ForeignKey("UserId")]
        public virtual Users User { get; set; }  // Changed from Users to User for consistency
    }
}