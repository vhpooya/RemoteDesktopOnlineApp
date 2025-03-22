using Microsoft.AspNetCore.Http;
using RemoteDesktopOnlineApps.Models;
using System.Threading.Tasks;

namespace RemoteDesktopOnlineApps.Services
{
    /// <summary>
    /// سرویس انتقال فایل بین کلاینت و سرور
    /// </summary>
    public interface IFileTransferService
    {
        /// <summary>
        /// شروع آپلود فایل از سرور پشتیبانی به سیستم مشتری
        /// </summary>
        Task<FileTransfer> InitiateFileUploadAsync(int sessionId, IFormFile file, string destinationPath);

        /// <summary>
        /// شروع دانلود فایل از سیستم مشتری به سرور پشتیبانی
        /// </summary>
        Task<FileTransfer> InitiateFileDownloadAsync(int sessionId, string remotePath, string localDestinationPath);

        /// <summary>
        /// دریافت لیست فایل‌ها و پوشه‌های یک مسیر در سیستم مشتری
        /// </summary>
        Task<List<FileSystemItem>> GetDirectoryContentsAsync(int sessionId, string path);

        /// <summary>
        /// لغو یک انتقال فایل در حال انجام
        /// </summary>
        Task CancelFileTransferAsync(string transferId);

        /// <summary>
        /// بروزرسانی پیشرفت انتقال فایل
        /// </summary>
        Task UpdateTransferProgressAsync(string transferId, long bytesTransferred, int progressPercentage);

        /// <summary>
        /// تکمیل یک انتقال فایل
        /// </summary>
        Task CompleteTransferAsync(string transferId);

        /// <summary>
        /// دریافت لیست انتقال‌های فایل یک جلسه
        /// </summary>
        Task<List<FileTransfer>> GetSessionFileTransfersAsync(int sessionId);

        /// <summary>
        /// دریافت اطلاعات یک انتقال فایل
        /// </summary>
        Task<FileTransfer> GetFileTransferAsync(string transferId);

        /// <summary>
        /// پردازش پاسخ دریافتی محتوای پوشه از کلاینت
        /// </summary>
        void ProcessDirectoryContentsResponse(string requestId, FileSystemInfo[] contents);
    }
    /// <summary>
    /// کلاس اطلاعات فایل‌های فایل سیستم
    /// </summary>
    public class FileSystemInfo
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public long Size { get; set; }
        public bool IsDirectory { get; set; }
        public DateTime LastModified { get; set; }
    }
}