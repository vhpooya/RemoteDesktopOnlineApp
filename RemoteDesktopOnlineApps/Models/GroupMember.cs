using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RemoteDesktopOnlineApps.Models
{
    /// <summary>
    /// مدل عضویت در گروه (برای اعلان‌های گروهی)
    /// </summary>
    public class GroupMember
    {
        /// <summary>
        /// شناسه
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// نام گروه
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string GroupName { get; set; }

        /// <summary>
        /// شناسه کاربر
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// زمان عضویت
        /// </summary>
        public DateTime JoinTime { get; set; } = DateTime.Now;

        /// <summary>
        /// زمان خروج (اختیاری)
        /// </summary>
        public DateTime? LeaveTime { get; set; }

        /// <summary>
        /// وضعیت فعال بودن
        /// </summary>
        [NotMapped]
        public bool IsActive => !LeaveTime.HasValue;

        /// <summary>
        /// نقش کاربر در گروه
        /// </summary>
        [MaxLength(50)]
        public string Role { get; set; }

        /// <summary>
        /// رابطه با کاربر
        /// </summary>
      
        public virtual Users User { get; set; }
    }
}