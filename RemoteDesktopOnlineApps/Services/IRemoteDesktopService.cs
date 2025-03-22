using RemoteDesktopOnlineApps.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RemoteDesktopOnlineApps.Services
{
    public interface IRemoteDesktopService
    {
        /// <summary>
        /// ایجاد یک جلسه ریموت جدید
        /// </summary>
        /// <param name="userId">شناسه کاربر پشتیبانی</param>
        /// <param name="serverIdentifier">شناسه سرور مشتری</param>
        /// <param name="serverName">نام سرور مشتری</param>
        /// <returns>جلسه ایجاد شده</returns>
        Task<RemoteSession> InitiateSessionAsync(int userId, string serverIdentifier, string serverName,string password);

        /// <summary>
        /// برقراری ارتباط با سرور مشتری
        /// </summary>
        /// <param name="sessionId">شناسه جلسه</param>
        /// <param name="password">رمز عبور سرور</param>
        /// <returns>وضعیت اتصال</returns>
        Task<bool> ConnectAsync(int sessionId, string password);

        /// <summary>
        /// قطع ارتباط با سرور مشتری
        /// </summary>
        /// <param name="sessionId">شناسه جلسه</param>
        Task DisconnectAsync(int sessionId);

        /// <summary>
        /// دعوت از کاربر دیگر برای پیوستن به جلسه
        /// </summary>
        /// <param name="sessionId">شناسه جلسه</param>
        /// <param name="participantId">شناسه کاربر مورد نظر</param>
        /// <returns>وضعیت دعوت</returns>
        Task<bool> InviteParticipantAsync(int sessionId, int participantId);

        /// <summary>
        /// دریافت لیست جلسات اخیر کاربر
        /// </summary>
        /// <param name="userId">شناسه کاربر</param>
        /// <param name="count">تعداد جلسات درخواستی</param>
        /// <returns>لیست جلسات</returns>
        Task<List<RemoteSession>> GetRecentSessionsAsync(int userId, int count = 10);

        /// <summary>
        /// ذخیره رمز عبور برای جلسه
        /// </summary>
        /// <param name="sessionId">شناسه جلسه</param>
        /// <param name="encryptedPassword">رمز عبور رمزنگاری شده</param>
        Task SaveSessionCredentialsAsync(int sessionId, string encryptedPassword);

        /// <summary>
        /// دریافت رمز عبور ذخیره شده برای جلسه
        /// </summary>
        /// <param name="sessionId">شناسه جلسه</param>
        /// <returns>رمز عبور رمزگشایی شده</returns>
        Task<string> GetSavedPasswordAsync(int sessionId);

        /// <summary>
        /// بررسی وضعیت آنلاین بودن سرور
        /// </summary>
        /// <param name="serverIdentifier">شناسه سرور</param>
        /// <returns>وضعیت آنلاین بودن</returns>
        Task<bool> CheckServerOnlineStatusAsync(string serverIdentifier);

        /// <summary>
        /// دریافت آمار و اطلاعات سرور
        /// </summary>
        /// <param name="sessionId">شناسه جلسه</param>
        /// <returns>اطلاعات سرور</returns>
        Task<ServerInfo> GetServerInfoAsync(int sessionId);
    }

    /// <summary>
    /// کلاس اطلاعات سرور
    /// </summary>
    public class ServerInfo
    {
        public string OperatingSystem { get; set; }
        public string ComputerName { get; set; }
        public string UserName { get; set; }
        public long TotalMemory { get; set; }
        public long AvailableMemory { get; set; }
        public int CpuUsage { get; set; }
        public List<DriveInfo> Drives { get; set; }

        public class DriveInfo
        {
            public string Name { get; set; }
            public long TotalSize { get; set; }
            public long FreeSpace { get; set; }
        }
    }
}