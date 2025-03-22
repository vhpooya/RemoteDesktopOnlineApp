using System;
using System.ComponentModel.DataAnnotations;

namespace RemoteDesktopOnlineApps.Models
{
    /// <summary>
    /// مدل اطلاعات سرور
    /// </summary>
    public class ServerInfo
    {
        /// <summary>
        /// شناسه
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// شناسه سرور
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string ServerIdentifier { get; set; }

        /// <summary>
        /// نام سرور
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string ServerName { get; set; }

        /// <summary>
        /// مالک سرور
        /// </summary>
        [MaxLength(100)]
        public string Owner { get; set; }

        /// <summary>
        /// توضیحات
        /// </summary>
        [MaxLength(500)]
        public string Description { get; set; }

        /// <summary>
        /// وضعیت فعال بودن سرور
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// آدرس IP سرور
        /// </summary>
        [MaxLength(50)]
        public string IpAddress { get; set; }

        /// <summary>
        /// پورت سرور
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// زمان ثبت سرور
        /// </summary>
        public DateTime RegisteredAt { get; set; } = DateTime.Now;

        /// <summary>
        /// آخرین زمان اتصال به سرور
        /// </summary>
        public DateTime? LastConnectionTime { get; set; }
    }
}