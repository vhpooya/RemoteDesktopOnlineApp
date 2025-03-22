using Microsoft.AspNetCore.SignalR;
using RemoteDesktopOnlineApps.Hubs;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace RemoteDesktopOnlineApps.Services
{
    public class WebRTCSignalingService : IWebRTCSignalingService
    {
        private readonly IHubContext<RemoteSessionHub> _remoteSessionHub;
        private readonly ILogger<WebRTCSignalingService> _logger;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> _connectionResponses = new();
        private readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> _pingResponses = new();

        public WebRTCSignalingService(
            IHubContext<RemoteSessionHub> remoteSessionHub,
            ILogger<WebRTCSignalingService> logger)
        {
            _remoteSessionHub = remoteSessionHub;
            _logger = logger;
        }

        public async Task<bool> EstablishConnectionAsync(int sessionId, string serverIdentifier, string password)
        {
            try
            {
                var requestId = Guid.NewGuid().ToString();
                var tcs = new TaskCompletionSource<bool>();
                _connectionResponses[requestId] = tcs;

                // ارسال درخواست اتصال به سرور مشتری با شناسه درخواست
                await _remoteSessionHub.Clients.Group($"server_{serverIdentifier}")
                    .SendAsync("ConnectionRequest", sessionId, password, requestId);

                // ایجاد تایمر برای تایم‌اوت
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                cts.Token.Register(() =>
                {
                    tcs.TrySetResult(false);
                    _connectionResponses.TryRemove(requestId, out _);
                    _logger.LogWarning("Connection request timed out for session {SessionId} to server {ServerIdentifier}", sessionId, serverIdentifier);
                }, useSynchronizationContext: false);

                // انتظار برای پاسخ
                var result = await tcs.Task;
                _logger.LogInformation("Connection request for session {SessionId} to server {ServerIdentifier} resulted in {Result}",
                    sessionId, serverIdentifier, result ? "success" : "failure");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error establishing connection for session {SessionId} to server {ServerIdentifier}",
                    sessionId, serverIdentifier);
                return false;
            }
        }

        // این متد توسط RemoteSessionHub فراخوانی می‌شود وقتی پاسخ اتصال دریافت می‌شود
        public void HandleConnectionResponse(string requestId, bool accepted)
        {
            if (_connectionResponses.TryRemove(requestId, out var tcs))
            {
                tcs.TrySetResult(accepted);
            }
        }

        public async Task CloseConnectionAsync(int sessionId)
        {
            try
            {
                _logger.LogInformation("Closing connection for session {SessionId}", sessionId);
                await _remoteSessionHub.Clients.Group($"session_{sessionId}")
                    .SendAsync("CloseConnection");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing connection for session {SessionId}", sessionId);
                throw;
            }
        }

        public async Task AddParticipantAsync(int sessionId, int participantId)
        {
            try
            {
                _logger.LogInformation("Adding participant {ParticipantId} to session {SessionId}", participantId, sessionId);

                // اضافه کردن شرکت‌کننده به گروه SignalR
                await _remoteSessionHub.Groups.AddToGroupAsync(GetConnectionIdForParticipant(participantId), $"session_{sessionId}");

                // اطلاع‌رسانی به سایر شرکت‌کنندگان
                await _remoteSessionHub.Clients.Group($"session_{sessionId}")
                    .SendAsync("ParticipantJoined", participantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding participant {ParticipantId} to session {SessionId}", participantId, sessionId);
                throw;
            }
        }

        // این متد باید در جایی پیاده‌سازی شود که به ConnectionId دسترسی دارید
        private string GetConnectionIdForParticipant(int participantId)
        {
            // در پیاده‌سازی واقعی، ConnectionId باید از یک سرویس یا کش بازیابی شود
            // این فقط یک نمونه است
            return $"connection_{participantId}";
        }

        public async Task<bool> PingServerAsync(string serverIdentifier)
        {
            try
            {
                var requestId = Guid.NewGuid().ToString();
                var tcs = new TaskCompletionSource<bool>();
                _pingResponses[requestId] = tcs;

                // ارسال پینگ به سرور
                await _remoteSessionHub.Clients.Group($"server_{serverIdentifier}")
                    .SendAsync("Ping", requestId);

                // ایجاد تایمر برای تایم‌اوت
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                cts.Token.Register(() =>
                {
                    tcs.TrySetResult(false);
                    _pingResponses.TryRemove(requestId, out _);
                }, useSynchronizationContext: false);

                // انتظار برای پاسخ
                return await tcs.Task;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pinging server {ServerIdentifier}", serverIdentifier);
                return false;
            }
        }

        // این متد توسط RemoteSessionHub فراخوانی می‌شود وقتی پاسخ پینگ دریافت می‌شود
        public void HandlePingResponse(string requestId)
        {
            if (_pingResponses.TryRemove(requestId, out var tcs))
            {
                tcs.TrySetResult(true);
            }
        }

        public async Task SendOfferAsync(int sessionId, string offer, string targetConnectionId = null)
        {
            try
            {
                _logger.LogDebug("Sending WebRTC offer to session {SessionId}", sessionId);

                if (targetConnectionId != null)
                {
                    // ارسال پیشنهاد به یک اتصال خاص
                    await _remoteSessionHub.Clients.Client(targetConnectionId)
                        .SendAsync("ReceiveOffer", offer);
                }
                else
                {
                    // ارسال پیشنهاد به تمام شرکت‌کنندگان جلسه
                    await _remoteSessionHub.Clients.Group($"session_{sessionId}")
                        .SendAsync("ReceiveOffer", offer);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending WebRTC offer to session {SessionId}", sessionId);
                throw;
            }
        }

        public async Task SendAnswerAsync(int sessionId, string answer, string targetConnectionId = null)
        {
            try
            {
                _logger.LogDebug("Sending WebRTC answer to session {SessionId}", sessionId);

                if (targetConnectionId != null)
                {
                    // ارسال پاسخ به یک اتصال خاص
                    await _remoteSessionHub.Clients.Client(targetConnectionId)
                        .SendAsync("ReceiveAnswer", answer);
                }
                else
                {
                    // ارسال پاسخ به تمام شرکت‌کنندگان جلسه
                    await _remoteSessionHub.Clients.Group($"session_{sessionId}")
                        .SendAsync("ReceiveAnswer", answer);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending WebRTC answer to session {SessionId}", sessionId);
                throw;
            }
        }

        public async Task SendIceCandidateAsync(int sessionId, string candidate, string targetConnectionId = null)
        {
            try
            {
                _logger.LogDebug("Sending ICE candidate to session {SessionId}", sessionId);

                if (targetConnectionId != null)
                {
                    // ارسال کاندیدای ICE به یک اتصال خاص
                    await _remoteSessionHub.Clients.Client(targetConnectionId)
                        .SendAsync("ReceiveIceCandidate", candidate);
                }
                else
                {
                    // ارسال کاندیدای ICE به تمام شرکت‌کنندگان جلسه
                    await _remoteSessionHub.Clients.Group($"session_{sessionId}")
                        .SendAsync("ReceiveIceCandidate", candidate);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending ICE candidate to session {SessionId}", sessionId);
                throw;
            }
        }

        public async Task RemoveParticipantAsync(int sessionId, int participantId)
        {
            try
            {
                _logger.LogInformation("Removing participant {ParticipantId} from session {SessionId}", participantId, sessionId);

                // حذف شرکت‌کننده از گروه SignalR
                await _remoteSessionHub.Groups.RemoveFromGroupAsync(GetConnectionIdForParticipant(participantId), $"session_{sessionId}");

                // اطلاع‌رسانی به سایر شرکت‌کنندگان
                await _remoteSessionHub.Clients.Group($"session_{sessionId}")
                    .SendAsync("ParticipantLeft", participantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing participant {ParticipantId} from session {SessionId}", participantId, sessionId);
                throw;
            }
        }
    }
}