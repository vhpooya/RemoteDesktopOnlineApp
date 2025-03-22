using Microsoft.EntityFrameworkCore;
using RemoteDesktopOnlineApps.Helpers;
using RemoteDesktopOnlineApps.Models;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RemoteDesktopOnlineApps.Services
{
    /// <summary>
    /// سرویس مدیریت دسترسی‌ها و مجوزها
    /// </summary>
    public class AuthorizationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthorizationService(
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// بررسی دسترسی کاربر به سرور
        /// </summary>
        /// <param name="userId">شناسه کاربر</param>
        /// <param name="serverIdentifier">شناسه سرور</param>
        /// <returns>وضعیت دسترسی</returns>
        public async Task<bool> CanAccessServerAsync(int userId, string serverIdentifier)
        {
            // بررسی نقش کاربر
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            // بررسی آیا کاربر مدیر است
            bool isAdmin = user.RoleName == RoleTitle.مدیر || _httpContextAccessor.HttpContext?.User?.GetRole() == "مدیر";
            if (isAdmin)
                return true; // مدیر دسترسی کامل دارد

            // بررسی دسترسی‌های ویژه کاربر
            var hasAccess = await _context.ServerAccess
                .AnyAsync(a => a.UserId == userId && a.ServerIdentifier == serverIdentifier);

            return hasAccess;
        }

        /// <summary>
        /// بررسی دسترسی غیرهمزمان به سرور
        /// </summary>
        /// <param name="userId">شناسه کاربر</param>
        /// <param name="serverIdentifier">شناسه سرور</param>
        /// <returns>وضعیت دسترسی</returns>
        public async Task<bool> CanAccessUnattendedAsync(int userId, string serverIdentifier)
        {
            // بررسی نقش کاربر
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            // بررسی آیا کاربر مدیر است
            bool isAdmin = user.RoleName == RoleTitle.مدیر || _httpContextAccessor.HttpContext?.User?.GetRole() == "مدیر";
            if (isAdmin)
                return true; // مدیر دسترسی کامل دارد

            // بررسی دسترسی‌های ویژه کاربر
            var serverAccess = await _context.ServerAccess
                .FirstOrDefaultAsync(a => a.UserId == userId && a.ServerIdentifier == serverIdentifier);

            return serverAccess != null && serverAccess.AllowUnattendedAccess;
        }

        /// <summary>
        /// دریافت لیست سرورهایی که کاربر به آنها دسترسی دارد
        /// </summary>
        /// <param name="userId">شناسه کاربر</param>
        /// <returns>لیست سرورها</returns>
        public async Task<List<string>> GetAccessibleServersAsync(int userId)
        {
            // بررسی نقش کاربر
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return new List<string>();

            // بررسی آیا کاربر مدیر است
            bool isAdmin = user.RoleName == RoleTitle.مدیر || _httpContextAccessor.HttpContext?.User?.GetRole() == "مدیر";
            if (isAdmin)
            {
                // مدیر به همه سرورها دسترسی دارد
                return await _context.ServerInfos
                    .Select(s => s.ServerIdentifier)
                    .ToListAsync();
            }

            // بررسی آیا کاربر راهبر است
            bool isAdministrator = user.RoleName == RoleTitle.راهبر || _httpContextAccessor.HttpContext?.User?.GetRole() == "راهبر";
            if (isAdministrator)
            {
                // راهبر به همه سرورهای خاصی دسترسی دارد (این قسمت را می‌توانید بر اساس نیازتان تغییر دهید)
                return await _context.ServerInfos
                    .Where(s => s.IsActive)
                    .Select(s => s.ServerIdentifier)
                    .ToListAsync();
            }

            // دریافت سرورهایی که کاربر به آنها دسترسی دارد
            return await _context.ServerAccess
                .Where(a => a.UserId == userId)
                .Select(a => a.ServerIdentifier)
                .ToListAsync();
        }

        /// <summary>
        /// اعطای دسترسی به کاربر برای یک سرور
        /// </summary>
        /// <param name="userId">شناسه کاربر</param>
        /// <param name="serverIdentifier">شناسه سرور</param>
        /// <param name="allowUnattended">اجازه دسترسی غیرهمزمان</param>
        /// <returns>نتیجه اعطای دسترسی</returns>
        public async Task<bool> GrantServerAccessAsync(int userId, string serverIdentifier, bool allowUnattended = false)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            var existingAccess = await _context.ServerAccess
                .FirstOrDefaultAsync(a => a.UserId == userId && a.ServerIdentifier == serverIdentifier);

            if (existingAccess != null)
            {
                // به‌روزرسانی دسترسی موجود
                existingAccess.AllowUnattendedAccess = allowUnattended;
                existingAccess.LastUpdatedAt = System.DateTime.Now;
            }
            else
            {
                // ایجاد دسترسی جدید
                _context.ServerAccess.Add(new ServerAccess
                {
                    UserId = userId,
                    ServerIdentifier = serverIdentifier,
                    AllowUnattendedAccess = allowUnattended,
                    CreatedAt = System.DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// لغو دسترسی کاربر به یک سرور
        /// </summary>
        /// <param name="userId">شناسه کاربر</param>
        /// <param name="serverIdentifier">شناسه سرور</param>
        /// <returns>نتیجه لغو دسترسی</returns>
        public async Task<bool> RevokeServerAccessAsync(int userId, string serverIdentifier)
        {
            var access = await _context.ServerAccess
                .FirstOrDefaultAsync(a => a.UserId == userId && a.ServerIdentifier == serverIdentifier);

            if (access == null)
                return false;

            _context.ServerAccess.Remove(access);
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// دریافت کاربر فعلی
        /// </summary>
        /// <returns>اطلاعات کاربر فعلی</returns>
        public async Task<Users> GetCurrentUserAsync()
        {
            if (_httpContextAccessor.HttpContext == null)
                return null;

            var userId = _httpContextAccessor.HttpContext.User.GetUserId();
            if (userId > 0)
            {
                return await _context.Users.FindAsync(userId);
            }

            return null;
        }

        /// <summary>
        /// دریافت شناسه کاربر فعلی
        /// </summary>
        /// <returns>شناسه کاربر فعلی</returns>
        public int GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext?.User?.GetUserId() ?? 0;
        }

        /// <summary>
        /// بررسی آیا کاربر فعلی در نقش مشخص شده قرار دارد
        /// </summary>
        /// <param name="role">نقش مورد نظر</param>
        /// <returns>وضعیت بررسی</returns>
        public bool IsCurrentUserInRole(RoleTitle role)
        {
            var currentRole = _httpContextAccessor.HttpContext?.User?.GetRole();
            return currentRole == role.ToString();
        }
    }
}