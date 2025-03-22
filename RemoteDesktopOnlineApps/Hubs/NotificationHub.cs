using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace RemoteDesktopOnlineApps.Hubs
{
    /// <summary>
    /// هاب اعلان‌ها و هشدارها
    /// </summary>
    public class NotificationHub : Hub
    {
        /// <summary>
        /// پیوستن کاربر به گروه اعلان
        /// </summary>
        /// <param name="groupName">نام گروه</param>
        /// <returns>Task</returns>
        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"notification_{groupName}");
        }

        /// <summary>
        /// خروج کاربر از گروه اعلان
        /// </summary>
        /// <param name="groupName">نام گروه</param>
        /// <returns>Task</returns>
        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"notification_{groupName}");
        }

        /// <summary>
        /// پاسخ به درخواست ریموت دسکتاپ
        /// </summary>
        /// <param name="requestId">شناسه درخواست</param>
        /// <param name="accepted">وضعیت پذیرش</param>
        /// <returns>Task</returns>
        public async Task RespondToRemoteRequest(int requestId, bool accepted)
        {
            await Clients.All.SendAsync("RemoteRequestResponse", requestId, accepted);
        }

        /// <summary>
        /// رویداد اتصال کاربر
        /// </summary>
        /// <returns>Task</returns>
        public override async Task OnConnectedAsync()
        {
            if (Context.User?.Identity?.IsAuthenticated == true)
            {
                var userId = Convert.ToInt32(Context.UserIdentifier);
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            }

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// رویداد قطع اتصال کاربر
        /// </summary>
        /// <param name="exception">استثنای احتمالی</param>
        /// <returns>Task</returns>
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            if (Context.User?.Identity?.IsAuthenticated == true)
            {
                var userId = Convert.ToInt32(Context.UserIdentifier);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}