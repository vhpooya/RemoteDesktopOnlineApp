using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RemoteDesktopOnlineApps.Helpers;
using RemoteDesktopOnlineApps.Models;
using RemoteDesktopOnlineApps.Services;
using RemoteDesktopOnlineApps.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RemoteDesktopOnlineApps.Controllers
{
    [Authorize]
    public class FileTransferController : Controller
    {
        private readonly IFileTransferService _fileTransferService;
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public FileTransferController(
            IFileTransferService fileTransferService,
            ApplicationDbContext context,
            ICurrentUserService currentUserService)
        {
            _fileTransferService = fileTransferService;
            _context = context;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// نمایش صفحه اصلی انتقال فایل
        /// </summary>
        public async Task<IActionResult> Index(int? sessionId)
        {
            var currentUserId = User.GetUserId();
            var viewModel = new FileTransferViewModel
            {
                CurrentSessionId = sessionId
            };

            // دریافت جلسات فعال کاربر
            viewModel.ActiveSessions = await _context.RemoteSessions
                .Where(s => s.UserId == currentUserId && s.Status == "Active")
                .OrderByDescending(s => s.StartTime)
                .ToListAsync();

            // اگر جلسه‌ای مشخص شده، انتقال‌های فایل آن را دریافت می‌کنیم
            if (sessionId.HasValue)
            {
                var session = await _context.RemoteSessions
                    .FirstOrDefaultAsync(s => s.Id == sessionId.Value && s.UserId == currentUserId);

                if (session == null)
                {
                    return NotFound();
                }

                viewModel.CurrentSession = session;
                viewModel.FileTransfers = await _fileTransferService.GetSessionFileTransfersAsync(sessionId.Value);
            }

            return View(viewModel);
        }

        /// <summary>
        /// نمایش صفحه آپلود فایل
        /// </summary>
        public async Task<IActionResult> Upload(int sessionId)
        {
            var currentUserId = User.GetUserId();
            var session = await _context.RemoteSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == currentUserId);

            if (session == null || session.Status != "Active")
            {
                return RedirectToAction("Index", new { sessionId });
            }

            var viewModel = new FileUploadViewModel
            {
                SessionId = sessionId,
                ServerName = session.ServerName
            };

            return View(viewModel);
        }

        /// <summary>
        /// پردازش درخواست آپلود فایل
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Upload(FileUploadViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var currentUserId = User.GetUserId();
            var session = await _context.RemoteSessions
                .FirstOrDefaultAsync(s => s.Id == model.SessionId && s.UserId == currentUserId);

            if (session == null || session.Status != "Active")
            {
                ModelState.AddModelError("", "جلسه موردنظر یافت نشد یا فعال نیست");
                return View(model);
            }

            try
            {
                if (model.Files != null && model.Files.Count > 0)
                {
                    foreach (var file in model.Files)
                    {
                        if (file.Length > 0)
                        {
                            await _fileTransferService.InitiateFileUploadAsync(
                                model.SessionId,
                                file,
                                model.DestinationPath);
                        }
                    }

                    TempData["SuccessMessage"] = $"{model.Files.Count} فایل برای آپلود به سرور ارسال شد";
                    return RedirectToAction("Index", new { sessionId = model.SessionId });
                }
                else
                {
                    ModelState.AddModelError("", "لطفاً حداقل یک فایل انتخاب کنید");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"خطا در آپلود فایل: {ex.Message}");
                return View(model);
            }
        }

        /// <summary>
        /// نمایش صفحه دانلود فایل
        /// </summary>
        public async Task<IActionResult> Download(int sessionId)
        {
            var currentUserId = User.GetUserId();
            var session = await _context.RemoteSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == currentUserId);

            if (session == null || session.Status != "Active")
            {
                return RedirectToAction("Index", new { sessionId });
            }

            var viewModel = new FileDownloadViewModel
            {
                SessionId = sessionId,
                ServerName = session.ServerName,
                LocalDestinationPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
            };

            return View(viewModel);
        }

        /// <summary>
        /// پردازش درخواست دانلود فایل
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Download(FileDownloadViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var currentUserId = User.GetUserId();
            var session = await _context.RemoteSessions
                .FirstOrDefaultAsync(s => s.Id == model.SessionId && s.UserId == currentUserId);

            if (session == null || session.Status != "Active")
            {
                ModelState.AddModelError("", "جلسه موردنظر یافت نشد یا فعال نیست");
                return View(model);
            }

            try
            {
                await _fileTransferService.InitiateFileDownloadAsync(
                    model.SessionId,
                    model.RemoteFilePath,
                    model.LocalDestinationPath);

                TempData["SuccessMessage"] = "درخواست دانلود فایل با موفقیت ارسال شد";
                return RedirectToAction("Index", new { sessionId = model.SessionId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"خطا در دانلود فایل: {ex.Message}");
                return View(model);
            }
        }

        /// <summary>
        /// مرور محتوای پوشه در سیستم مشتری
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> BrowseDirectory(int sessionId, string path)
        {
            var currentUserId = User.GetUserId();
            var session = await _context.RemoteSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == currentUserId);

            if (session == null || session.Status != "Active")
            {
                return Json(new { success = false, error = "جلسه موردنظر یافت نشد یا فعال نیست" });
            }

            try
            {
                var contents = await _fileTransferService.GetDirectoryContentsAsync(sessionId, path);
                return Json(new { success = true, contents });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// لغو یک انتقال فایل در حال انجام
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CancelTransfer(string transferId)
        {
            try
            {
                var transfer = await _fileTransferService.GetFileTransferAsync(transferId);

                var session = await _context.RemoteSessions
                    .FirstOrDefaultAsync(s => s.Id == transfer.RemoteSessionId);

                if (session != null && session.UserId == User.GetUserId())
                {
                    await _fileTransferService.CancelFileTransferAsync(transferId);
                    return Json(new { success = true });
                }

                return Json(new { success = false, error = "دسترسی غیرمجاز" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// دریافت اطلاعات پیشرفت یک انتقال فایل
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTransferProgress(string transferId)
        {
            try
            {
                var transfer = await _fileTransferService.GetFileTransferAsync(transferId);

                var session = await _context.RemoteSessions
                    .FirstOrDefaultAsync(s => s.Id == transfer.RemoteSessionId);

                if (session != null && session.UserId == User.GetUserId())
                {
                    return Json(new
                    {
                        success = true,
                        transfer.FileName,
                        transfer.FileSize,
                        transfer.ProgressPercentage,
                        transfer.Status,
                        transfer.StartTime,
                        transfer.EndTime,
                        transfer.IsUpload
                    });
                }

                return Json(new { success = false, error = "دسترسی غیرمجاز" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// دریافت لیست انتقال‌های فایل یک جلسه
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetSessionTransfers(int sessionId)
        {
            var currentUserId = User.GetUserId();
            var session = await _context.RemoteSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == currentUserId);

            if (session == null)
            {
                return Json(new { success = false, error = "جلسه موردنظر یافت نشد" });
            }

            try
            {
                var transfers = await _fileTransferService.GetSessionFileTransfersAsync(sessionId);

                var result = transfers.Select(t => new
                {
                    t.TransferId,
                    t.FileName,
                    t.FileSize,
                    t.ProgressPercentage,
                    t.Status,
                    StartTime = t.StartTime.ToString("yyyy/MM/dd HH:mm:ss"),
                    EndTime = t.EndTime.HasValue ? t.EndTime.Value.ToString("yyyy/MM/dd HH:mm:ss") : null,
                    t.IsUpload,
                    Duration = t.EndTime.HasValue ?
                        (t.EndTime.Value - t.StartTime).TotalSeconds.ToString("F1") + " ثانیه" :
                        "در حال انتقال"
                });

                return Json(new { success = true, transfers = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
    }
}