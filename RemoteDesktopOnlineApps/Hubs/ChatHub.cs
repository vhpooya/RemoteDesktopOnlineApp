using Microsoft.AspNetCore.SignalR;
using RemoteDesktopOnlineApps.Models;
using System;
using System.Threading.Tasks;

namespace RemoteDesktopOnlineApps.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public ChatHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task JoinChatRoom(int sessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_{sessionId}");
        }

        public async Task LeaveChatRoom(int sessionId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat_{sessionId}");
        }

        public async Task SendMessage(int sessionId, string message, bool isFromSupport)
        {
            var senderName = Context.User.Identity.Name;
            var senderId = Convert.ToInt32(Context.UserIdentifier);

            var chatMessage = new ChatMessage
            {
                RemoteSessionId = sessionId,
                SenderId = senderId,
                SenderName = senderName,
                Message = message,
                Timestamp = DateTime.Now,
                IsFromSupport = isFromSupport
            };

            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

            await Clients.Group($"chat_{sessionId}").SendAsync("ReceiveMessage",
                chatMessage.Id, senderName, message, chatMessage.Timestamp, isFromSupport);
        }
    }
}