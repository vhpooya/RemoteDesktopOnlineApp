using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using RemoteDesktopOnlineApps.Hubs;
using RemoteDesktopOnlineApps.Models;
using RemoteDesktopOnlineApps.Services;
using System;
using System.Threading.Tasks;

namespace RemoteDesktopOnlineApps.Extensions
{
    public static class SignalRExtensions
    {
        public static IServiceCollection AddRemoteDesktopSignalR(this IServiceCollection services)
        {
            // افزودن SignalR
            services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
                options.MaximumReceiveMessageSize = 102400000; // 100MB برای انتقال فایل
                options.StreamBufferCapacity = 20;
            })
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.PropertyNamingPolicy = null;
            });

            // افزودن سرویس آمار اتصال
            services.AddSingleton<IRemoteDesktopStatsService, RemoteDesktopStatsService>();

            return services;
        }

        public static IApplicationBuilder UseRemoteDesktopSignalR(this IApplicationBuilder app)
        {
            // پیکربندی مسیرهای هاب
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<RemoteSessionHub>("/remoteSessionHub");
                endpoints.MapHub<ChatHub>("/chatHub");
                endpoints.MapHub<NotificationHub>("/notificationHub");
                endpoints.MapHub<ConferenceHub>("/conferenceHub");
            });

            return app;
        }
    }

    public static class RemoteSessionHubExtensions
    {
        // دریافت آمار WebRTC از کلاینت
        public static async Task SendWebRTCStats(this Hub hub, int sessionId, string statsJson)
        {
            try
            {
                var statsService = hub.Context.GetHttpContext().RequestServices
                    .GetRequiredService<IRemoteDesktopStatsService>();

                // تبدیل JSON به آبجکت آمار اتصال
                var stats = System.Text.Json.JsonSerializer.Deserialize<RemoteConnectionStats>(statsJson);
                stats.SessionId = sessionId;

                // محاسبه کیفیت اتصال بر اساس آمار
                stats.ConnectionQuality = CalculateConnectionQuality(stats);

                // بروزرسانی و ارسال آمار
                await statsService.UpdateConnectionStatsAsync(sessionId, stats);
            }
            catch (Exception ex)
            {
                // لاگ خطا
                var logger = hub.Context.GetHttpContext().RequestServices
                    .GetRequiredService<Microsoft.Extensions.Logging.ILogger<RemoteSessionHub>>();
                logger.LogError(ex, "خطا در پردازش آمار WebRTC");
            }
        }

        // محاسبه کیفیت اتصال بر اساس آمار
        private static ConnectionQuality CalculateConnectionQuality(RemoteConnectionStats stats)
        {
            // پارامترهای موثر در کیفیت اتصال
            double latencyScore = GetLatencyScore(stats.LatencyMs);
            double packetLossScore = GetPacketLossScore(stats.PacketLoss);
            double bandwidthScore = GetBandwidthScore(stats.BandwidthUsage);
            double fpsScore = GetFPSScore(stats.FPS);

            // محاسبه امتیاز کلی
            double overallScore = (latencyScore * 0.3) + (packetLossScore * 0.3) +
                                (bandwidthScore * 0.2) + (fpsScore * 0.2);

            // تعیین کیفیت بر اساس امتیاز
            if (overallScore >= 0.8) return ConnectionQuality.Excellent;
            if (overallScore >= 0.6) return ConnectionQuality.Good;
            if (overallScore >= 0.4) return ConnectionQuality.Fair;
            return ConnectionQuality.Poor;
        }

        private static double GetLatencyScore(int latencyMs)
        {
            if (latencyMs <= 50) return 1.0;  // عالی
            if (latencyMs <= 100) return 0.8; // خوب
            if (latencyMs <= 200) return 0.6; // قابل قبول
            if (latencyMs <= 300) return 0.4; // ضعیف
            return 0.2;                       // خیلی ضعیف
        }

        private static double GetPacketLossScore(double packetLoss)
        {
            if (packetLoss <= 0.5) return 1.0;  // عالی
            if (packetLoss <= 1.0) return 0.8;  // خوب
            if (packetLoss <= 2.0) return 0.6;  // قابل قبول
            if (packetLoss <= 5.0) return 0.4;  // ضعیف
            return 0.2;                          // خیلی ضعیف
        }

        private static double GetBandwidthScore(double bandwidthKbps)
        {
            if (bandwidthKbps >= 5000) return 1.0;  // عالی
            if (bandwidthKbps >= 2000) return 0.8;  // خوب
            if (bandwidthKbps >= 1000) return 0.6;  // قابل قبول
            if (bandwidthKbps >= 500) return 0.4;   // ضعیف
            return 0.2;                             // خیلی ضعیف
        }

        private static double GetFPSScore(double fps)
        {
            if (fps >= 25) return 1.0;  // عالی
            if (fps >= 15) return 0.8;  // خوب
            if (fps >= 10) return 0.6;  // قابل قبول
            if (fps >= 5) return 0.4;   // ضعیف
            return 0.2;                 // خیلی ضعیف
        }
    }
}