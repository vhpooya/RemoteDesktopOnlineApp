using RemoteDesktopOnlineApps.Models;

namespace RemoteDesktopOnlineApps.ViewModels
{
    /// <summary>
    /// ViewModel برای صفحه جلسه ریموت
    /// </summary>
    public class RemoteSessionViewModel
    {
        /// <summary>
        /// اطلاعات جلسه ریموت
        /// </summary>
        public RemoteSession Session { get; set; }

        /// <summary>
        /// آمار اتصال
        /// </summary>
        public RemoteConnectionStats ConnectionStats { get; set; }
    }
}