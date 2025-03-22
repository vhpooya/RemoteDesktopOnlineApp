using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using RemoteDesktopOnlineApps.Hubs;
using RemoteDesktopOnlineApps.Models;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteDesktopOnlineApps.Services
{
    public interface IRemoteDesktopStatsService
    {
        Task StartMonitoringSessionAsync(int sessionId);
        Task StopMonitoringSessionAsync(int sessionId);
        Task UpdateConnectionStatsAsync(int sessionId, RemoteConnectionStats stats);
        Task<RemoteConnectionStats> GetConnectionStatsAsync(int sessionId);
    }

}