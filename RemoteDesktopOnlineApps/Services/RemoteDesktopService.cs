using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RemoteDesktopOnlineApps.Helpers;
using RemoteDesktopOnlineApps.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RemoteDesktopOnlineApps.Services
{
    public class RemoteDesktopService : IRemoteDesktopService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEncryptionService _encryptionService;
        private readonly IWebRTCSignalingService _signalingService;

        public RemoteDesktopService(
            ApplicationDbContext context,
            IEncryptionService encryptionService,
            IWebRTCSignalingService signalingService)
        {
            _context = context;
            _encryptionService = encryptionService;
            _signalingService = signalingService;
        }

        public async Task<RemoteSession> InitiateSessionAsync(int userId, string serverIdentifier, string serverName, string password)
        {
            // ایجاد یک شناسه تصادفی برای جلسه
            var sessionId = new Random().Next(100000, 999999);

            var session = new RemoteSession
            {
                SessionId = sessionId, // تنظیم شناسه یکتای جلسه
                UserId = userId,
                ServerIdentifier = serverIdentifier,
                ServerName = serverName,
                StartTime = DateTime.Now,
                Status = "Initializing",
                ConnectionType = "RemoteControl",
                Notes = "توضیحات",
                IsPasswordSaved = true,
                SavedPasswordHash = AppHelpers.HashPassword(password)
            };

           

            _context.RemoteSessions.Add(session);
            await _context.SaveChangesAsync();

            return session;
        }

        public async Task<bool> ConnectAsync(int sessionId, string password)
        {
            var session = await _context.RemoteSessions.FindAsync(sessionId);
            if (session == null)
                return false;

            // اینجا لاجیک برقراری ارتباط با سرور مشتری با استفاده از WebRTC
            bool connected = await _signalingService.EstablishConnectionAsync(
                session.SessionId,
                session.ServerIdentifier,
                password);

            if (connected)
            {
                session.Status = "Active";
                await _context.SaveChangesAsync();
            }
            else
            {
                session.Status = "Failed";
                await _context.SaveChangesAsync();
            }

            return connected;
        }

        public async Task DisconnectAsync(int sessionId)
        {
            var session = await _context.RemoteSessions.FindAsync(sessionId);
            if (session != null)
            {
                session.EndTime = DateTime.Now;
                session.Status = "Disconnected";
                await _context.SaveChangesAsync();

                await _signalingService.CloseConnectionAsync(session.SessionId);
            }
        }

        public async Task<bool> InviteParticipantAsync(int sessionId, int participantId)
        {
            var session = await _context.RemoteSessions.FindAsync(sessionId);
            if (session == null || session.Status != "Active")
                return false;

            var participant = new SessionParticipant
            {
                RemoteSessionId = sessionId,
                UserId = participantId,
                JoinTime = DateTime.Now
            };

            _context.SessionParticipants.Add(participant);
            await _context.SaveChangesAsync();

            await _signalingService.AddParticipantAsync(session.SessionId, participantId);
            return true;
        }

        public async Task<List<RemoteSession>> GetRecentSessionsAsync(int userId, int count = 10)
        {
            return await _context.RemoteSessions
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.StartTime)
                .Take(count)
                .ToListAsync();
        }

        public async Task SaveSessionCredentialsAsync(int sessionId, string password)
        {
            var session = await _context.RemoteSessions.FindAsync(sessionId);
            if (session != null)
            {
                session.IsPasswordSaved = true;
                session.SavedPasswordHash = _encryptionService.EncryptPassword(password);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<string> GetSavedPasswordAsync(int sessionId)
        {
            var session = await _context.RemoteSessions.FindAsync(sessionId);
            if (session != null && session.IsPasswordSaved)
            {
                return _encryptionService.DecryptPassword(session.SavedPasswordHash);
            }
            return null;
        }

        public async Task<bool> CheckServerOnlineStatusAsync(string serverIdentifier)
        {
            // اینجا لاجیک بررسی وضعیت آنلاین بودن سرور
            return await _signalingService.PingServerAsync(serverIdentifier);
        }

        public async Task<ServerInfo> GetServerInfoAsync(int sessionId)
        {
            var session = await _context.RemoteSessions.FindAsync(sessionId);
            if (session == null || session.Status != "Active")
                return null;

            // در یک پیاده‌سازی واقعی، این اطلاعات از سرور مشتری دریافت می‌شود
            // اینجا برای نمونه مقادیر ثابت برگردانده می‌شود
            return new ServerInfo
            {
                OperatingSystem = "Windows Server 2019",
                ComputerName = session.ServerName,
                UserName = "Administrator",
                TotalMemory = 16384, // MB
                AvailableMemory = 8192, // MB
                CpuUsage = 25, // درصد
                Drives = new List<ServerInfo.DriveInfo>
                {
                    new ServerInfo.DriveInfo
                    {
                        Name = "C:",
                        TotalSize = 500 * 1024, // MB
                        FreeSpace = 250 * 1024 // MB
                    },
                    new ServerInfo.DriveInfo
                    {
                        Name = "D:",
                        TotalSize = 1000 * 1024, // MB
                        FreeSpace = 800 * 1024 // MB
                    }
                }
            };
        }
    }
}