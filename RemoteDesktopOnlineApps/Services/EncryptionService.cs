using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace RemoteDesktopOnlineApps.Services
{
    public class EncryptionService : IEncryptionService
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public EncryptionService(IConfiguration configuration)
        {
            // خواندن کلید رمزنگاری از تنظیمات
            string encryptionKey = configuration["EncryptionSettings:Key"];
            string encryptionIv = configuration["EncryptionSettings:IV"];

            if (string.IsNullOrEmpty(encryptionKey) || string.IsNullOrEmpty(encryptionIv))
            {
                throw new ArgumentException("تنظیمات رمزنگاری در فایل پیکربندی یافت نشد.");
            }

            _key = Convert.FromBase64String(encryptionKey);
            _iv = Convert.FromBase64String(encryptionIv);
        }

        public string EncryptPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return null;

            byte[] encryptedBytes = EncryptData(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(encryptedBytes);
        }

        public string DecryptPassword(string encryptedPassword)
        {
            if (string.IsNullOrEmpty(encryptedPassword))
                return null;

            byte[] encryptedBytes = Convert.FromBase64String(encryptedPassword);
            byte[] decryptedBytes = DecryptData(encryptedBytes);
            return Encoding.UTF8.GetString(decryptedBytes);
        }

        public byte[] EncryptData(byte[] data)
        {
            if (data == null || data.Length == 0)
                return null;

            using (var aes = Aes.Create())
            {
                aes.Key = _key;
                aes.IV = _iv;

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(data, 0, data.Length);
                        cs.FlushFinalBlock();
                    }

                    return ms.ToArray();
                }
            }
        }

        public byte[] DecryptData(byte[] encryptedData)
        {
            if (encryptedData == null || encryptedData.Length == 0)
                return null;

            using (var aes = Aes.Create())
            {
                aes.Key = _key;
                aes.IV = _iv;

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(encryptedData, 0, encryptedData.Length);
                        cs.FlushFinalBlock();
                    }

                    return ms.ToArray();
                }
            }
        }

        public string GenerateEncryptionKey()
        {
            using (var aes = Aes.Create())
            {
                aes.GenerateKey();
                return Convert.ToBase64String(aes.Key);
            }
        }

        public string ComputeHash(string data)
        {
            if (string.IsNullOrEmpty(data))
                return null;

            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
                return Convert.ToBase64String(hashBytes);
            }
        }

        public bool VerifyHash(string data, string hash)
        {
            if (string.IsNullOrEmpty(data) || string.IsNullOrEmpty(hash))
                return false;

            string computedHash = ComputeHash(data);
            return computedHash == hash;
        }
    }
}