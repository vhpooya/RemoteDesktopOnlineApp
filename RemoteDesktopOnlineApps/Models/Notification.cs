using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RemoteDesktopOnlineApps.Models
{
    /// <summary>
    /// مدل اعلان
    /// </summary>
    public class Notification
    {
        /// <summary>
        /// شناسه اعلان
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// شناسه کاربر دریافت‌کننده اعلان
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// نام گروه دریافت‌کننده اعلان (اختیاری)
        /// </summary>
        public string GroupName { get; set; }

        /// <summary>
        /// عنوان اعلان
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Title { get; set; }

        /// <summary>
        /// متن اعلان
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Message { get; set; }

        /// <summary>
        /// نوع اعلان (Info, Success, Warning, Error, RemoteRequest, FileTransfer, Chat)
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Type { get; set; }

        /// <summary>
        /// زمان ایجاد اعلان
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// آیا اعلان خوانده شده است؟
        /// </summary>
        public bool IsRead { get; set; }

        /// <summary>
        /// زمان خواندن اعلان
        /// </summary>
        public DateTime? ReadTime { get; set; }

        /// <summary>
        /// آیا اعلان سراسری است؟
        /// </summary>
        public bool IsGlobal { get; set; }

        /// <summary>
        /// شناسه درخواست‌کننده (برای اعلان‌های RemoteRequest)
        /// </summary>
        public string RequesterId { get; set; }

        /// <summary>
        /// داده‌های اضافی (به فرمت JSON)
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// رابطه با کاربر
        /// </summary>
       
        public virtual Users User { get; set; }
    }
}