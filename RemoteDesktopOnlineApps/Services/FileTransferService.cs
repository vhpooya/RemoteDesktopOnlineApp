using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RemoteDesktopOnlineApps.Hubs;
using RemoteDesktopOnlineApps.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteDesktopOnlineApps.Services
{
    public class FileTransferService : IFileTransferService, IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<RemoteSessionHub> _remoteSessionHub;
        private readonly ILogger<FileTransferService> _logger;

        // Dictionary برای نگهداری و مدیریت callback‌های مربوط به درخواست‌های مسیرها و فایل‌ها
        private readonly ConcurrentDictionary<string, Action<FileSystemInfo[]>> _directoryRequestCallbacks;
        private readonly ConcurrentDictionary<string, Action<byte[]>> _fileDownloadCallbacks;

        // Dictionary برای نگهداری وضعیت انتقال‌های فایل در حال انجام
        private readonly ConcurrentDictionary<string, FileTransferStatus> _activeTransfers;

        public FileTransferService(
            ApplicationDbContext context,
            IHubContext<RemoteSessionHub> remoteSessionHub,
            ILogger<FileTransferService> logger)
        {
            _context = context;
            _remoteSessionHub = remoteSessionHub;
            _logger = logger;
            _directoryRequestCallbacks = new ConcurrentDictionary<string, Action<FileSystemInfo[]>>();
            _fileDownloadCallbacks = new ConcurrentDictionary<string, Action<byte[]>>();
            _activeTransfers = new ConcurrentDictionary<string, FileTransferStatus>();
        }

        /// <summary>
        /// شروع آپلود فایل از سرور پشتیبانی به سیستم مشتری
        /// </summary>
        public async Task<FileTransfer> InitiateFileUploadAsync(int sessionId, IFormFile file, string destinationPath)
        {
            var session = await _context.RemoteSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
            {
                throw new ArgumentException($"جلسه با شناسه {sessionId} یافت نشد", nameof(sessionId));
            }

            _logger.LogInformation($"شروع آپلود فایل {file.FileName} به مسیر {destinationPath} در جلسه {sessionId}");

            // ایجاد رکورد انتقال فایل در دیتابیس
            var transferId = Guid.NewGuid().ToString();
            var fileTransfer = new FileTransfer
            {
                TransferId = transferId,
                RemoteSessionId = sessionId,
                FileName = file.FileName,
                SourcePath = "Local Upload",
                DestinationPath = destinationPath,
                FileSize = file.Length,
                StartTime = DateTime.Now,
                IsUpload = true,
                Status = "Initializing",
                ProgressPercentage = 0
            };

            _context.FileTransfers.Add(fileTransfer);
            await _context.SaveChangesAsync();

            try
            {
                // اطلاع‌رسانی به کلاینت برای آماده‌سازی دریافت فایل
                await _remoteSessionHub.Clients.Group($"session_{session.SessionId}")
                    .SendAsync("InitiateFileUpload", new
                    {
                        transferId,
                        fileName = file.FileName,
                        fileSize = file.Length,
                        destinationPath
                    });

                // ایجاد وضعیت انتقال و ذخیره آن
                var transferStatus = new FileTransferStatus
                {
                    TransferId = transferId,
                    SessionId = session.SessionId,
                    FileName = file.FileName,
                    TotalSize = file.Length,
                    StartTime = DateTime.Now
                };

                _activeTransfers.TryAdd(transferId, transferStatus);

                // خواندن محتوای فایل
                using (var ms = new MemoryStream())
                {
                    await file.CopyToAsync(ms);
                    ms.Position = 0;

                    byte[] fileData = ms.ToArray();
                    int chunkSize = 64 * 1024; // 64 KB
                    int totalChunks = (int)Math.Ceiling(fileData.Length / (double)chunkSize);

                    _logger.LogInformation($"فایل {file.FileName} به {totalChunks} بخش تقسیم شد");

                    // آپدیت وضعیت
                    fileTransfer.Status = "Transferring";
                    fileTransfer.Chunks = totalChunks;
                    await _context.SaveChangesAsync();

                    // ارسال داده‌ها به صورت چانک
                    for (int i = 0; i < totalChunks; i++)
                    {
                        // بررسی لغو شدن انتقال
                        if (_activeTransfers.TryGetValue(transferId, out var status) && status.IsCancelled)
                        {
                            _logger.LogInformation($"انتقال فایل {file.FileName} لغو شد");
                            fileTransfer.Status = "Cancelled";
                            fileTransfer.EndTime = DateTime.Now;
                            await _context.SaveChangesAsync();
                            return fileTransfer;
                        }

                        int start = i * chunkSize;
                        int length = Math.Min(chunkSize, fileData.Length - start);
                        byte[] chunk = new byte[length];
                        Buffer.BlockCopy(fileData, start, chunk, 0, length);

                        // ارسال چانک به کلاینت مقصد
                        await _remoteSessionHub.Clients.Group($"session_{session.SessionId}")
                            .SendAsync("FileUploadChunk", new
                            {
                                transferId,
                                chunkIndex = i,
                                totalChunks,
                                chunkData = Convert.ToBase64String(chunk),
                                isLastChunk = (i == totalChunks - 1)
                            });

                        // بروزرسانی پیشرفت در دیتابیس
                        fileTransfer.ProgressPercentage = (int)((i + 1) * 100.0 / totalChunks);
                        await _context.SaveChangesAsync();

                        // بروزرسانی و اطلاع‌رسانی پیشرفت
                        if (_activeTransfers.TryGetValue(transferId, out status))
                        {
                            status.BytesTransferred = start + length;
                            status.ProgressPercentage = fileTransfer.ProgressPercentage;

                            // ارسال اطلاعات پیشرفت به کلاینت
                            await _remoteSessionHub.Clients.Group($"session_{session.SessionId}")
                                .SendAsync("FileTransferProgress", new
                                {
                                    transferId,
                                    bytesTransferred = status.BytesTransferred,
                                    totalSize = status.TotalSize,
                                    progress = status.ProgressPercentage,
                                    speed = CalculateTransferSpeed(status),
                                    estimatedTimeRemaining = CalculateTimeRemaining(status)
                                });
                        }

                        // تاخیر کوتاه برای جلوگیری از اشباع
                        await Task.Delay(10);
                    }

                    // آپدیت وضعیت نهایی
                    fileTransfer.Status = "Completed";
                    fileTransfer.EndTime = DateTime.Now;
                    await _context.SaveChangesAsync();

                    // اطلاع‌رسانی اتمام انتقال
                    await _remoteSessionHub.Clients.Group($"session_{session.SessionId}")
                        .SendAsync("FileTransferCompleted", new { transferId });

                    // حذف از لیست انتقال‌های فعال
                    _activeTransfers.TryRemove(transferId, out _);

                    _logger.LogInformation($"آپلود فایل {file.FileName} با موفقیت تکمیل شد");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"خطا در آپلود فایل {file.FileName}: {ex.Message}");

                // ثبت خطا در دیتابیس
                fileTransfer.Status = "Failed";
                fileTransfer.ErrorMessage = ex.Message;
                fileTransfer.EndTime = DateTime.Now;
                await _context.SaveChangesAsync();

                // اطلاع‌رسانی خطا به کلاینت
                await _remoteSessionHub.Clients.Group($"session_{session.SessionId}")
                    .SendAsync("FileTransferError", new
                    {
                        transferId,
                        error = ex.Message
                    });

                // حذف از لیست انتقال‌های فعال
                _activeTransfers.TryRemove(transferId, out _);

                throw;
            }

            return fileTransfer;
        }

        /// <summary>
        /// شروع دانلود فایل از سیستم مشتری به سرور پشتیبانی
        /// </summary>
        public async Task<FileTransfer> InitiateFileDownloadAsync(int sessionId, string remotePath, string localDestinationPath)
        {
            var session = await _context.RemoteSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
            {
                throw new ArgumentException($"جلسه با شناسه {sessionId} یافت نشد", nameof(sessionId));
            }

            _logger.LogInformation($"شروع دانلود فایل از مسیر {remotePath} به {localDestinationPath} در جلسه {sessionId}");

            // استخراج نام فایل از مسیر
            string fileName = Path.GetFileName(remotePath);

            // ایجاد رکورد انتقال فایل در دیتابیس
            var transferId = Guid.NewGuid().ToString();
            var fileTransfer = new FileTransfer
            {
                TransferId = transferId,
                RemoteSessionId = sessionId,
                FileName = fileName,
                SourcePath = remotePath,
                DestinationPath = localDestinationPath,
                FileSize = 0, // در ابتدا سایز نامشخص است
                StartTime = DateTime.Now,
                IsUpload = false,
                Status = "Initializing",
                ProgressPercentage = 0
            };

            _context.FileTransfers.Add(fileTransfer);
            await _context.SaveChangesAsync();

            try
            {
                // ایجاد TaskCompletionSource برای منتظر ماندن اتمام دانلود
                var downloadCompleteTcs = new TaskCompletionSource<bool>();

                // ایجاد وضعیت انتقال
                var transferStatus = new FileTransferStatus
                {
                    TransferId = transferId,
                    SessionId = session.SessionId,
                    FileName = fileName,
                    StartTime = DateTime.Now,
                    CompletionTask = downloadCompleteTcs.Task
                };

                _activeTransfers.TryAdd(transferId, transferStatus);

                // مسیر محلی کامل فایل
                string localFullPath = Path.Combine(localDestinationPath, fileName);

                // ایجاد مسیر در صورت عدم وجود
                Directory.CreateDirectory(localDestinationPath);

                using (var fileStream = new FileStream(localFullPath, FileMode.Create, FileAccess.Write))
                {
                    // تنظیم callback برای دریافت محتوای فایل
                    using (var cts = new CancellationTokenSource(TimeSpan.FromMinutes(30))) // تایم‌اوت 30 دقیقه
                    {
                        // ثبت یک callback برای دریافت بخش‌های فایل
                        var callbackId = $"download_{transferId}";
                        var fileChunks = new ConcurrentDictionary<int, byte[]>();
                        int totalChunks = -1; // مقدار اولیه نامشخص

                        // تابع callback برای پردازش بخش‌های دریافتی
                        async Task HandleChunkReceived(int chunkIndex, int chunksTotal, byte[] chunkData, bool isLastChunk)
                        {
                            if (totalChunks == -1)
                            {
                                totalChunks = chunksTotal;

                                // آپدیت وضعیت بر اساس اطلاعات دریافتی
                                transferStatus.TotalSize = chunksTotal * 64 * 1024; // تخمین اولیه
                                transferStatus.TotalChunks = chunksTotal;

                                // بروزرسانی رکورد دیتابیس
                                fileTransfer.Chunks = chunksTotal;
                                fileTransfer.Status = "Transferring";
                                await _context.SaveChangesAsync();
                            }

                            // ذخیره چانک
                            fileChunks[chunkIndex] = chunkData;

                            // نوشتن چانک‌های دریافتی به صورت ترتیبی
                            int nextChunkToWrite = 0;
                            while (fileChunks.TryRemove(nextChunkToWrite, out var chunk))
                            {
                                await fileStream.WriteAsync(chunk, 0, chunk.Length);
                                await fileStream.FlushAsync();

                                // بروزرسانی وضعیت
                                transferStatus.BytesTransferred += chunk.Length;
                                transferStatus.ProgressPercentage = (int)((nextChunkToWrite + 1) * 100.0 / totalChunks);

                                // بروزرسانی رکورد دیتابیس
                                fileTransfer.FileSize = transferStatus.BytesTransferred; // بروزرسانی سایز فایل
                                fileTransfer.ProgressPercentage = transferStatus.ProgressPercentage;
                                await _context.SaveChangesAsync();

                                // اطلاع‌رسانی پیشرفت
                                await _remoteSessionHub.Clients.Group($"session_{session.SessionId}")
                                    .SendAsync("FileTransferProgress", new
                                    {
                                        transferId,
                                        bytesTransferred = transferStatus.BytesTransferred,
                                        totalSize = transferStatus.TotalSize,
                                        progress = transferStatus.ProgressPercentage,
                                        speed = CalculateTransferSpeed(transferStatus),
                                        estimatedTimeRemaining = CalculateTimeRemaining(transferStatus)
                                    });

                                nextChunkToWrite++;
                            }

                            // بررسی تکمیل شدن دانلود
                            if (isLastChunk && nextChunkToWrite == totalChunks)
                            {
                                // تکمیل دانلود
                                fileTransfer.Status = "Completed";
                                fileTransfer.EndTime = DateTime.Now;
                                await _context.SaveChangesAsync();

                                // اطلاع‌رسانی اتمام
                                await _remoteSessionHub.Clients.Group($"session_{session.SessionId}")
                                    .SendAsync("FileTransferCompleted", new { transferId });

                                // تکمیل TaskCompletionSource
                                downloadCompleteTcs.TrySetResult(true);
                            }
                        }

                        // ثبت متد برای پردازش بخش‌های فایل
                        RegisterFileChunkHandler(callbackId, HandleChunkReceived);

                        // ارسال درخواست دانلود فایل به کلاینت
                        await _remoteSessionHub.Clients.Group($"session_{session.SessionId}")
                            .SendAsync("InitiateFileDownload", new
                            {
                                transferId,
                                callbackId,
                                filePath = remotePath
                            });

                        // منتظر ماندن برای تکمیل دانلود یا تایم‌اوت
                        var timeoutTask = Task.Delay(TimeSpan.FromMinutes(30));
                        var completedTask = await Task.WhenAny(downloadCompleteTcs.Task, timeoutTask);

                        if (completedTask == timeoutTask)
                        {
                            _logger.LogWarning($"تایم‌اوت در دانلود فایل {fileName}");
                            fileTransfer.Status = "Failed";
                            fileTransfer.ErrorMessage = "تایم‌اوت در دانلود فایل";
                            fileTransfer.EndTime = DateTime.Now;
                            await _context.SaveChangesAsync();

                            // اطلاع‌رسانی خطا
                            await _remoteSessionHub.Clients.Group($"session_{session.SessionId}")
                                .SendAsync("FileTransferError", new
                                {
                                    transferId,
                                    error = "تایم‌اوت در دانلود فایل"
                                });

                            throw new TimeoutException("تایم‌اوت در دانلود فایل");
                        }

                        // پاکسازی هندلر بخش فایل
                        UnregisterFileChunkHandler(callbackId);
                    }
                }

                // حذف از لیست انتقال‌های فعال
                _activeTransfers.TryRemove(transferId, out _);

                _logger.LogInformation($"دانلود فایل {fileName} با موفقیت تکمیل شد");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"خطا در دانلود فایل {fileName}: {ex.Message}");

                // ثبت خطا در دیتابیس
                fileTransfer.Status = "Failed";
                fileTransfer.ErrorMessage = ex.Message;
                fileTransfer.EndTime = DateTime.Now;
                await _context.SaveChangesAsync();

                // اطلاع‌رسانی خطا
                await _remoteSessionHub.Clients.Group($"session_{session.SessionId}")
                    .SendAsync("FileTransferError", new
                    {
                        transferId,
                        error = ex.Message
                    });

                // حذف از لیست انتقال‌های فعال
                _activeTransfers.TryRemove(transferId, out _);

                throw;
            }

            return fileTransfer;
        }

        /// <summary>
        /// دریافت لیست فایل‌ها و پوشه‌های یک مسیر در سیستم مشتری
        /// </summary>
        public async Task<List<FileSystemItem>> GetDirectoryContentsAsync(int sessionId, string path)
        {
            var session = await _context.RemoteSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
            {
                throw new ArgumentException($"جلسه با شناسه {sessionId} یافت نشد", nameof(sessionId));
            }

            _logger.LogInformation($"درخواست محتوای پوشه {path} در جلسه {sessionId}");

            // ایجاد TaskCompletionSource برای منتظر ماندن پاسخ
            var tcs = new TaskCompletionSource<List<FileSystemItem>>();

            // شناسه یکتا برای این درخواست
            string requestId = Guid.NewGuid().ToString();

            try
            {
                // ثبت یک callback برای دریافت پاسخ
                _directoryRequestCallbacks[requestId] = (result) =>
                {
                    var fileSystemItems = result
                        .Select(item => new FileSystemItem
                        {
                            Name = item.Name,
                            Path = item.Path,
                            Extension = Path.GetExtension(item.Name),
                            IsDirectory = item is DirectoryInfo,
                            Size = item is FileSystemInfo ? ((FileSystemInfo)item).Size : 0,
                            LastModified = item.LastModified
                        })
                        .ToList();

                    tcs.TrySetResult(fileSystemItems);
                };

                // تنظیم تایمر برای تایم‌اوت
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                {
                    cts.Token.Register(() =>
                    {
                        tcs.TrySetException(new TimeoutException($"درخواست محتوای پوشه {path} با تایم‌اوت مواجه شد"));
                        _directoryRequestCallbacks.TryRemove(requestId, out _);
                    });

                    // ارسال درخواست به کلاینت
                    await _remoteSessionHub.Clients.Group($"session_{session.SessionId}")
                        .SendAsync("RequestDirectoryContents", new
                        {
                            requestId,
                            path
                        });

                    // منتظر ماندن برای پاسخ یا تایم‌اوت
                    return await tcs.Task;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"خطا در دریافت محتوای پوشه {path}: {ex.Message}");

                // حذف callback در صورت خطا
                _directoryRequestCallbacks.TryRemove(requestId, out _);
                throw;
            }
        }

        /// <summary>
        /// لغو یک انتقال فایل در حال انجام
        /// </summary>
        public async Task CancelFileTransferAsync(string transferId)
        {
            if (_activeTransfers.TryGetValue(transferId, out var transferStatus))
            {
                _logger.LogInformation($"درخواست لغو انتقال فایل با شناسه {transferId}");

                // علامت‌گذاری انتقال به عنوان لغو شده
                transferStatus.IsCancelled = true;

                // اطلاع‌رسانی به کلاینت
                await _remoteSessionHub.Clients.Group($"session_{transferStatus.SessionId}")
                    .SendAsync("CancelFileTransfer", new { transferId });

                // بروزرسانی رکورد دیتابیس
                var fileTransfer = await _context.FileTransfers
                    .FirstOrDefaultAsync(f => f.TransferId == transferId);

                if (fileTransfer != null)
                {
                    fileTransfer.Status = "Cancelled";
                    fileTransfer.EndTime = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                throw new ArgumentException($"انتقال فایل با شناسه {transferId} یافت نشد یا قبلا به پایان رسیده است", nameof(transferId));
            }
        }

        /// <summary>
        /// دریافت لیست انتقال‌های فایل یک جلسه
        /// </summary>
        public async Task<List<FileTransfer>> GetSessionFileTransfersAsync(int sessionId)
        {
            return await _context.FileTransfers
                .Where(f => f.RemoteSessionId == sessionId)
                .OrderByDescending(f => f.StartTime)
                .ToListAsync();
        }

        /// <summary>
        /// دریافت اطلاعات یک انتقال فایل
        /// </summary>
        public async Task<FileTransfer> GetFileTransferAsync(string transferId)
        {
            var fileTransfer = await _context.FileTransfers
                .FirstOrDefaultAsync(f => f.TransferId == transferId);

            if (fileTransfer == null)
            {
                throw new ArgumentException($"انتقال فایل با شناسه {transferId} یافت نشد", nameof(transferId));
            }

            return fileTransfer;
        }

        /// <summary>
        /// پردازش پاسخ دریافتی محتوای پوشه از کلاینت
        /// </summary>
        public void ProcessDirectoryContentsResponse(string requestId, FileSystemInfo[] contents)
        {
            if (_directoryRequestCallbacks.TryRemove(requestId, out var callback))
            {
                callback(contents);
            }
            else
            {
                _logger.LogWarning($"درخواست محتوای پوشه با شناسه {requestId} یافت نشد یا منقضی شده است");
            }
        }

        /// <summary>
        /// ثبت handler برای پردازش بخش‌های فایل دریافتی
        /// </summary>
        public void RegisterFileChunkHandler(string callbackId, Func<int, int, byte[], bool, Task> handler)
        {
            // این متد در یک پیاده‌سازی واقعی باید یک dictionary از callback‌ها نگهداری کند
            // و handler را در آن ذخیره کند

            // همچنین باید یک متد در RemoteSessionHub وجود داشته باشد که
            // بخش‌های فایل دریافتی را به این handler ارسال کند
        }

        /// <summary>
        /// حذف handler بخش‌های فایل
        /// </summary>
        public void UnregisterFileChunkHandler(string callbackId)
        {
            // پاکسازی handler از dictionary
        }

        /// <summary>
        /// محاسبه سرعت انتقال فایل
        /// </summary>
        private string CalculateTransferSpeed(FileTransferStatus status)
        {
            double elapsedSeconds = (DateTime.Now - status.StartTime).TotalSeconds;
            if (elapsedSeconds <= 0) return "0 KB/s";

            double speedBytesPerSec = status.BytesTransferred / elapsedSeconds;

            if (speedBytesPerSec < 1024)
                return $"{speedBytesPerSec:F1} B/s";
            else if (speedBytesPerSec < 1024 * 1024)
                return $"{speedBytesPerSec / 1024:F1} KB/s";
            else
                return $"{speedBytesPerSec / (1024 * 1024):F1} MB/s";
        }

        /// <summary>
        /// محاسبه زمان تخمینی باقیمانده برای انتقال
        /// </summary>
        private string CalculateTimeRemaining(FileTransferStatus status)
        {
            if (status.BytesTransferred <= 0) return "محاسبه زمان...";

            double elapsedSeconds = (DateTime.Now - status.StartTime).TotalSeconds;
            if (elapsedSeconds <= 0) return "محاسبه زمان...";

            double bytesPerSecond = status.BytesTransferred / elapsedSeconds;
            if (bytesPerSecond <= 0) return "محاسبه زمان...";

            double remainingBytes = status.TotalSize - status.BytesTransferred;
            double remainingSeconds = remainingBytes / bytesPerSecond;

            if (remainingSeconds < 60)
                return $"{(int)remainingSeconds} ثانیه";
            else if (remainingSeconds < 3600)
                return $"{(int)(remainingSeconds / 60)} دقیقه و {(int)(remainingSeconds % 60)} ثانیه";
            else
                return $"{(int)(remainingSeconds / 3600)} ساعت و {(int)((remainingSeconds % 3600) / 60)} دقیقه";
        }

        /// <summary>
        /// آزادسازی منابع
        /// </summary>
        public void Dispose()
        {
            _directoryRequestCallbacks.Clear();
            _fileDownloadCallbacks.Clear();
            _activeTransfers.Clear();
        }

        /// <summary>
        /// بروزرسانی پیشرفت انتقال فایل
        /// </summary>
        public async Task UpdateTransferProgressAsync(string transferId, long bytesTransferred, int progressPercentage)
        {
            var fileTransfer = await _context.FileTransfers
                .FirstOrDefaultAsync(f => f.TransferId == transferId);

            if (fileTransfer == null)
            {
                throw new ArgumentException($"انتقال فایل با شناسه {transferId} یافت نشد", nameof(transferId));
            }

            fileTransfer.ProgressPercentage = progressPercentage;

            // اگر فقط در حال بروزرسانی پیشرفت هستیم، وضعیت را به Transferring تغییر دهیم
            if (fileTransfer.Status == "Initializing")
            {
                fileTransfer.Status = "Transferring";
            }

            await _context.SaveChangesAsync();

            // بروزرسانی وضعیت انتقال فعال
            if (_activeTransfers.TryGetValue(transferId, out var status))
            {
                status.BytesTransferred = bytesTransferred;

                // اطلاع‌رسانی پیشرفت
                await _remoteSessionHub.Clients.Group($"session_{status.SessionId}")
                    .SendAsync("FileTransferProgress", new
                    {
                        transferId,
                        bytesTransferred,
                        totalSize = status.TotalSize,
                        progress = progressPercentage,
                        speed = CalculateTransferSpeed(status),
                        estimatedTimeRemaining = CalculateTimeRemaining(status)
                    });
            }
        }

        /// <summary>
        /// تکمیل یک انتقال فایل
        /// </summary>
        public async Task CompleteTransferAsync(string transferId)
        {
            var fileTransfer = await _context.FileTransfers
                .FirstOrDefaultAsync(f => f.TransferId == transferId);

            if (fileTransfer == null)
            {
                throw new ArgumentException($"انتقال فایل با شناسه {transferId} یافت نشد", nameof(transferId));
            }

            fileTransfer.Status = "Completed";
            fileTransfer.ProgressPercentage = 100;
            fileTransfer.EndTime = DateTime.Now;
            await _context.SaveChangesAsync();

            // بروزرسانی وضعیت انتقال فعال
            if (_activeTransfers.TryRemove(transferId, out var status))
            {
                // اطلاع‌رسانی تکمیل
                await _remoteSessionHub.Clients.Group($"session_{status.SessionId}")
                    .SendAsync("FileTransferCompleted", new { transferId });
            }
        }

        /// <summary>
        /// کلاس کمکی برای نگهداری وضعیت انتقال‌های فایل
        /// </summary>
        private class FileTransferStatus
        {
            public string TransferId { get; set; }
            public int SessionId { get; set; }
            public string FileName { get; set; }
            public long TotalSize { get; set; }
            public long BytesTransferred { get; set; }
            private int _progressPercentage;
            public int ProgressPercentage
            {
                get => TotalSize > 0 ? (int)((BytesTransferred * 100) / TotalSize) : _progressPercentage;
                set => _progressPercentage = value;
            }
            public DateTime StartTime { get; set; }
            public bool IsCancelled { get; set; }
            public int TotalChunks { get; set; }
            public Task CompletionTask { get; set; }
        }
    }

    /// <summary>
    /// کلاس مدل برای نمایش آیتم‌های فایل سیستم
    /// </summary>
    public class FileSystemItem
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Extension { get; set; }
        public bool IsDirectory { get; set; }
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
    }
}