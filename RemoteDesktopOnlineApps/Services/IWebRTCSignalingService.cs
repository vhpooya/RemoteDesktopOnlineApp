using System.Threading.Tasks;

namespace RemoteDesktopOnlineApps.Services
{
    /// <summary>
    /// سرویس سیگنالینگ WebRTC برای برقراری ارتباط بین کلاینت‌ها
    /// </summary>
    public interface IWebRTCSignalingService
    {
        /// <summary>
        /// برقراری ارتباط با سرور مشتری
        /// </summary>
        /// <param name="sessionId">شناسه جلسه</param>
        /// <param name="serverIdentifier">شناسه سرور</param>
        /// <param name="password">رمز عبور اتصال</param>
        /// <returns>نتیجه اتصال</returns>
        Task<bool> EstablishConnectionAsync(int sessionId, string serverIdentifier, string password);

        /// <summary>
        /// بستن ارتباط جلسه
        /// </summary>
        /// <param name="sessionId">شناسه جلسه</param>
        Task CloseConnectionAsync(int sessionId);

        /// <summary>
        /// افزودن شرکت‌کننده به جلسه
        /// </summary>
        /// <param name="sessionId">شناسه جلسه</param>
        /// <param name="participantId">شناسه شرکت‌کننده</param>
        Task AddParticipantAsync(int sessionId, int participantId);

        /// <summary>
        /// حذف شرکت‌کننده از جلسه
        /// </summary>
        /// <param name="sessionId">شناسه جلسه</param>
        /// <param name="participantId">شناسه شرکت‌کننده</param>
        Task RemoveParticipantAsync(int sessionId, int participantId);

        /// <summary>
        /// ارسال پینگ به سرور برای بررسی در دسترس بودن
        /// </summary>
        /// <param name="serverIdentifier">شناسه سرور</param>
        /// <returns>نتیجه پینگ</returns>
        Task<bool> PingServerAsync(string serverIdentifier);

        /// <summary>
        /// ارسال پیشنهاد WebRTC
        /// </summary>
        /// <param name="sessionId">شناسه جلسه</param>
        /// <param name="offer">پیشنهاد WebRTC</param>
        /// <param name="targetConnectionId">شناسه اتصال هدف (اختیاری)</param>
        Task SendOfferAsync(int sessionId, string offer, string targetConnectionId = null);

        /// <summary>
        /// ارسال پاسخ WebRTC
        /// </summary>
        /// <param name="sessionId">شناسه جلسه</param>
        /// <param name="answer">پاسخ WebRTC</param>
        /// <param name="targetConnectionId">شناسه اتصال هدف (اختیاری)</param>
        Task SendAnswerAsync(int sessionId, string answer, string targetConnectionId = null);

        /// <summary>
        /// ارسال کاندید ICE
        /// </summary>
        /// <param name="sessionId">شناسه جلسه</param>
        /// <param name="candidate">کاندید ICE</param>
        /// <param name="targetConnectionId">شناسه اتصال هدف (اختیاری)</param>
        Task SendIceCandidateAsync(int sessionId, string candidate, string targetConnectionId = null);

        /// <summary>
        /// پردازش پاسخ اتصال
        /// </summary>
        /// <param name="requestId">شناسه درخواست</param>
        /// <param name="accepted">وضعیت پذیرش</param>
        void HandleConnectionResponse(string requestId, bool accepted);

        /// <summary>
        /// پردازش پاسخ پینگ
        /// </summary>
        /// <param name="requestId">شناسه درخواست</param>
        void HandlePingResponse(string requestId);
    }
}