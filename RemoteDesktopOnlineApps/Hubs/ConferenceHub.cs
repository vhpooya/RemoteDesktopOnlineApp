using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace RemoteDesktopOnlineApps.Hubs
{
    public class ConferenceHub : Hub
    {
        public async Task JoinConference(int sessionId, string userName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"conference_{sessionId}");
            await Clients.Group($"conference_{sessionId}").SendAsync("UserJoined",
                Context.ConnectionId, userName, DateTime.Now);
        }

        public async Task LeaveConference(int sessionId, string userName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conference_{sessionId}");
            await Clients.Group($"conference_{sessionId}").SendAsync("UserLeft",
                Context.ConnectionId, userName, DateTime.Now);
        }

        public async Task SendMediaOffer(int sessionId, string targetConnectionId, string offer)
        {
            await Clients.Client(targetConnectionId).SendAsync("ReceiveMediaOffer",
                Context.ConnectionId, offer);
        }

        public async Task SendMediaAnswer(string targetConnectionId, string answer)
        {
            await Clients.Client(targetConnectionId).SendAsync("ReceiveMediaAnswer",
                Context.ConnectionId, answer);
        }

        public async Task SendIceCandidate(string targetConnectionId, string candidate)
        {
            await Clients.Client(targetConnectionId).SendAsync("ReceiveIceCandidate",
                Context.ConnectionId, candidate);
        }
    }
}