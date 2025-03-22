using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RemoteDesktopOnlineApps.Helpers;
using RemoteDesktopOnlineApps.Models;
using System.Security.Claims;

namespace RemoteDesktopOnlineApps.Services
{
    public interface ICurrentUserService
    {
        Task<Users> GetCurrentUserAsync();
        int? GetCurrentUserId();
    }

    public class CurrentUserService : ICurrentUserService
    {
        private readonly ApplicationDbContext db;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(ApplicationDbContext _db, IHttpContextAccessor httpContextAccessor)
        {
            db = _db;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Users> GetCurrentUserAsync()
        {
            var userId = GetCurrentUserId();
            if (userId.HasValue)
            {
                return await db.Users.FindAsync(userId.Value);
            }
            return null;
        }

        public int? GetCurrentUserId()
        {
            int userId = _httpContextAccessor.HttpContext?.User?.GetUserId() ?? 0;
            return userId > 0 ? userId : null;
        }
    }

}
