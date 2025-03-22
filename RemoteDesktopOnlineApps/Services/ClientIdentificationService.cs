using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RemoteDesktopOnlineApps.Models;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RemoteDesktopOnlineApps.Services
{
    public interface IClientIdentificationService
    {
        Task<bool> RegisterClientAsync();
        Task<ClientConnectionInfo> GetConnectionInfoAsync();
        Task<bool> ValidateClientAsync(string clientId, string secretKey);
        Task<bool> UpdateClientStatusAsync(string clientId, bool isOnline);
    }

    public class ClientIdentificationService : IClientIdentificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ClientIdentificationService> _logger;
        private readonly IEncryptionService _encryptionService;

        // در واقعیت این مقادیر باید از تنظیمات یا ذخیره‌سازی ایمن خوانده شوند
        private string _clientId;
        private string _secretKey;

        public ClientIdentificationService(
            ApplicationDbContext context,
            ILogger<ClientIdentificationService> logger,
            IEncryptionService encryptionService)
        {
            _context = context;
            _logger = logger;
            _encryptionService = encryptionService;
        }

        public async Task<bool> RegisterClientAsync()
        {
            try
            {
                // بررسی وجود رجیستری قبلی
                var storedRegInfo = await GetStoredRegistrationInfoAsync();
                if (storedRegInfo != null)
                {
                    _clientId = storedRegInfo.ClientIdentifier;
                    _secretKey = storedRegInfo.Password;
                    _logger.LogInformation("اطلاعات کلاینت از قبل موجود است. ClientId: {ClientId}", _clientId);

                    // به‌روزرسانی آخرین زمان ارتباط
                    storedRegInfo.LastUpdated = DateTime.Now;
                    await _context.SaveChangesAsync();

                    return true;
                }

                // ایجاد شناسه جدید
                _clientId = GenerateClientId();
                _secretKey = GenerateSecretKey();

                // ذخیره اطلاعات
                var clientRegistration = new ClientRegistration
                {
                    ClientIdentifier = _clientId,
                    Password = _encryptionService.EncryptPassword(_secretKey),
                    MachineName = Environment.MachineName,
                    OperatingSystem = Environment.OSVersion.ToString(),
                    RegisteredDate = DateTime.Now,
                    LastUpdated = DateTime.Now,
                    IsActive = true
                };

                _context.ClientRegistrations.Add(clientRegistration);
                await _context.SaveChangesAsync();

                _logger.LogInformation("کلاینت جدید با موفقیت ثبت شد. ClientId: {ClientId}", _clientId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در ثبت کلاینت");
                return false;
            }
        }

        public async Task<ClientConnectionInfo> GetConnectionInfoAsync()
        {
            if (string.IsNullOrEmpty(_clientId))
            {
                await RegisterClientAsync();
            }

            return new ClientConnectionInfo
            {
                ClientIdentifier = _clientId,  // تغییر از ClientIdentifier به ClientId
                SecretKey = _secretKey,
                ConnectionPassword = _secretKey,  // استفاده از همان مقدار SecretKey
                MachineName = Environment.MachineName,
                ServerUrl = "wss://remote.example.com/signaling"  // آدرس سرور SignalR
            };
        }

        public async Task<bool> ValidateClientAsync(string clientId, string secretKey)
        {
            try
            {
                var client = await _context.ClientRegistrations
                    .FirstOrDefaultAsync(c => c.ClientIdentifier == clientId);

                if (client == null)
                    return false;

                // مقایسه کلید مخفی
                string decryptedKey = _encryptionService.DecryptPassword(client.Password);
                bool isValid = decryptedKey == secretKey;

                if (isValid)
                {
                    // به‌روزرسانی آخرین زمان ارتباط
                    client.LastUpdated = DateTime.Now;
                    await _context.SaveChangesAsync();
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در اعتبارسنجی کلاینت");
                return false;
            }
        }

        public async Task<bool> UpdateClientStatusAsync(string clientId, bool isOnline)
        {
            try
            {
                var client = await _context.ClientRegistrations
                    .FirstOrDefaultAsync(c => c.ClientIdentifier == clientId);

                if (client == null)
                    return false;

                client.IsActive = isOnline;
                client.LastUpdated = DateTime.Now;
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در به‌روزرسانی وضعیت کلاینت");
                return false;
            }
        }

        private async Task<ClientRegistration> GetStoredRegistrationInfoAsync()
        {
            return await _context.ClientRegistrations
                .Where(c => c.MachineName == Environment.MachineName)
                .OrderByDescending(c => c.RegisteredDate)
                .FirstOrDefaultAsync();
        }

        private string GenerateClientId()
        {
            // ایجاد یک شناسه 16 کاراکتری
            return Guid.NewGuid().ToString("N").Substring(0, 16);
        }

        private string GenerateSecretKey()
        {
            // ایجاد یک کلید مخفی 32 کاراکتری
            using (var rng = RandomNumberGenerator.Create())
            {
                var bytes = new byte[16];
                rng.GetBytes(bytes);
                return Convert.ToBase64String(bytes);
            }
        }
    }
}