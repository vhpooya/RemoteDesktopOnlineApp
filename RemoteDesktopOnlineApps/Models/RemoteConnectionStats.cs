using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RemoteDesktopOnlineApps.Models
{
    /// <summary>
    /// آمار و اطلاعات اتصال ریموت دسکتاپ
    /// </summary>
    public class RemoteConnectionStats
    {
        [Key]
        public int Id { get; set; }

        public int SessionId { get; set; }

        // کیفیت اتصال
        public ConnectionQuality ConnectionQuality { get; set; }

        // پهنای باند مصرفی (Kbps)
        public double BandwidthUsage { get; set; }

        // تأخیر (میلی‌ثانیه)
        public int LatencyMs { get; set; }

        // فریم در ثانیه
        public double FPS { get; set; }

        // درصد از دست رفتن بسته‌ها
        public double PacketLoss { get; set; }

        // وضوح تصویر
        public string Resolution { get; set; }

        // کدک تصویر
        public string VideoCodec { get; set; }

        // کدک صدا
        public string AudioCodec { get; set; }

        // تعداد شرکت‌کنندگان فعال
        public int ActiveParticipants { get; set; }

        // مدت زمان اتصال
        public TimeSpan ConnectedTime { get; set; }

        // زمان آخرین بروزرسانی
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        // میزان مصرف CPU
        public double CpuUsage { get; set; }

        // میزان مصرف RAM
        public double MemoryUsageMB { get; set; }

        // وضعیت اینترکانکشن ICE
        public string IceConnectionState { get; set; }

        // وضعیت سیگنالینگ
        public string SignalingState { get; set; }

        // Navigation property
      //  [ForeignKey("SessionId")]
        public virtual RemoteSession RemoteSession { get; set; }
    }

    /// <summary>
    /// انواع کیفیت اتصال
    /// </summary>
    public enum ConnectionQuality
    {
        Unknown,
        Poor,
        Fair,
        Good,
        Excellent
    }
}