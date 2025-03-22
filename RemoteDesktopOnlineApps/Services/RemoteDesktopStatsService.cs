using Microsoft.AspNetCore.SignalR;
using RemoteDesktopOnlineApps.Hubs;
using RemoteDesktopOnlineApps.Models;
using System.Collections.Concurrent;

namespace RemoteDesktopOnlineApps.Services
{
    public class RemoteDesktopStatsService : IRemoteDesktopStatsService, IDisposable
    {
        private readonly IHubContext<RemoteSessionHub> _remoteSessionHub;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RemoteDesktopStatsService> _logger;

        // کش برای نگهداری اطلاعات اتصال جلسات فعال
        private readonly ConcurrentDictionary<int, RemoteConnectionStats> _sessionStats = new ConcurrentDictionary<int, RemoteConnectionStats>();

        // تایمرها برای مانیتورینگ مستمر
        private readonly ConcurrentDictionary<int, Timer> _monitoringTimers = new ConcurrentDictionary<int, Timer>();

        public RemoteDesktopStatsService(
            IHubContext<RemoteSessionHub> remoteSessionHub,
            ApplicationDbContext context,
            ILogger<RemoteDesktopStatsService> logger)
        {
            _remoteSessionHub = remoteSessionHub;
            _context = context;
            _logger = logger;
        }

        public async Task StartMonitoringSessionAsync(int sessionId)
        {
            try
            {
                // ایجاد وضعیت اولیه
                var initialStats = new RemoteConnectionStats
                {
                    SessionId = sessionId,
                    ConnectionQuality = ConnectionQuality.Unknown,
                    BandwidthUsage = 0,
                    LatencyMs = 0,
                    FPS = 0,
                    PacketLoss = 0,
                    ConnectedTime = TimeSpan.Zero,
                    LastUpdated = DateTime.Now
                };

                _sessionStats.TryAdd(sessionId, initialStats);

                // راه‌اندازی تایمر برای به‌روزرسانی مستمر
                var timer = new Timer(
                    async (state) => await BroadcastStatsUpdateAsync(sessionId),
                    null,
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(1)
                );

                _monitoringTimers.TryAdd(sessionId, timer);

                _logger.LogInformation($"آغاز مانیتورینگ جلسه {sessionId}");

                // ارسال وضعیت اولیه
                await BroadcastStatsUpdateAsync(sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"خطا در آغاز مانیتورینگ جلسه {sessionId}");
            }
        }

        public async Task StopMonitoringSessionAsync(int sessionId)
        {
            try
            {
                // حذف و توقف تایمر
                if (_monitoringTimers.TryRemove(sessionId, out var timer))
                {
                    await timer.DisposeAsync();
                }

                // حذف اطلاعات جلسه
                _sessionStats.TryRemove(sessionId, out _);

                _logger.LogInformation($"پایان مانیتورینگ جلسه {sessionId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"خطا در پایان مانیتورینگ جلسه {sessionId}");
            }
        }

        public async Task UpdateConnectionStatsAsync(int sessionId, RemoteConnectionStats stats)
        {
            try
            {
                // بررسی وجود جلسه
                if (!_sessionStats.ContainsKey(sessionId))
                {
                    await StartMonitoringSessionAsync(sessionId);
                }

                // بروزرسانی آمار
                stats.LastUpdated = DateTime.Now;
                _sessionStats[sessionId] = stats;

                // ارسال به کلاینت‌ها
                await BroadcastStatsUpdateAsync(sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"خطا در بروزرسانی آمار جلسه {sessionId}");
            }
        }

        public Task<RemoteConnectionStats> GetConnectionStatsAsync(int sessionId)
        {
            if (_sessionStats.TryGetValue(sessionId, out var stats))
            {
                return Task.FromResult(stats);
            }

            return Task.FromResult(new RemoteConnectionStats
            {
                SessionId = sessionId,
                ConnectionQuality = ConnectionQuality.Unknown,
                BandwidthUsage = 0,
                LatencyMs = 0,
                FPS = 0,
                PacketLoss = 0,
                ConnectedTime = TimeSpan.Zero,
                LastUpdated = DateTime.Now
            });
        }

        private async Task BroadcastStatsUpdateAsync(int sessionId)
        {
            try
            {
                if (_sessionStats.TryGetValue(sessionId, out var stats))
                {
                    // بروزرسانی زمان اتصال
                    stats.ConnectedTime = stats.ConnectedTime.Add(TimeSpan.FromSeconds(1));
                    _sessionStats[sessionId] = stats;

                    // ارسال به کلاینت‌ها
                    await _remoteSessionHub.Clients.Group($"session_{sessionId}")
                        .SendAsync("ReceiveConnectionStats", stats);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"خطا در ارسال بروزرسانی آمار به کلاینت‌ها - جلسه {sessionId}");
            }
        }

        public void Dispose()
        {
            // توقف همه تایمرها
            foreach (var timer in _monitoringTimers.Values)
            {
                timer.Dispose();
            }

            _monitoringTimers.Clear();
            _sessionStats.Clear();
        }
    }

}
