using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace RemoteSupportPro.Web.Helpers
{
    public class EncryptionHelper
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public EncryptionHelper(string secretKey)
        {
            // استفاده از کلید رمزنگاری ثابت برای سادگی
            // در محیط واقعی باید کلید از تنظیمات برنامه خوانده شود و امن‌تر مدیریت شود
            using (var sha256 = SHA256.Create())
            {
                _key = sha256.ComputeHash(Encoding.UTF8.GetBytes(secretKey));
                // استفاده از 16 بایت اول برای بردار اولیه
                _iv = new byte[16];
                Array.Copy(_key, _iv, 16);
            }
        }

        /// <summary>
        /// رمزنگاری رشته با الگوریتم AES
        /// </summary>
        /// <param name="plainText">متن اصلی</param>
        /// <returns>متن رمزنگاری شده به صورت Base64</returns>
        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            byte[] encrypted;
            using (var aes = Aes.Create())
            {
                aes.Key = _key;
                aes.IV = _iv;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(cs))
                        {
                            sw.Write(plainText);
                        }
                        encrypted = ms.ToArray();
                    }
                }
            }

            return Convert.ToBase64String(encrypted);
        }

        /// <summary>
        /// رمزگشایی رشته رمزنگاری شده با الگوریتم AES
        /// </summary>
        /// <param name="cipherText">متن رمزنگاری شده به صورت Base64</param>
        /// <returns>متن اصلی</returns>
        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            try
            {
                byte[] cipherBytes = Convert.FromBase64String(cipherText);
                string plaintext = null;

                using (var aes = Aes.Create())
                {
                    aes.Key = _key;
                    aes.IV = _iv;

                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                    using (MemoryStream ms = new MemoryStream(cipherBytes))
                    {
                        using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader reader = new StreamReader(cs))
                            {
                                plaintext = reader.ReadToEnd();
                            }
                        }
                    }
                }

                return plaintext;
            }
            catch
            {
                // در صورت بروز خطا در رمزگشایی
                return null;
            }
        }

        /// <summary>
        /// تولید هش SHA256 از رشته
        /// </summary>
        /// <param name="text">رشته ورودی</param>
        /// <returns>رشته هش شده</returns>
        public string ComputeHash(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));

                // تبدیل بایت‌های هش به رشته هگزادسیمال
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    builder.Append(hashBytes[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }

        /// <summary>
        /// رمزنگاری فایل با الگوریتم AES
        /// </summary>
        /// <param name="inputFile">مسیر فایل ورودی</param>
        /// <param name="outputFile">مسیر فایل خروجی</param>
        public void EncryptFile(string inputFile, string outputFile)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = _key;
                aes.IV = _iv;

                using (FileStream fsInput = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
                {
                    using (FileStream fsOutput = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
                    {
                        ICryptoTransform encryptor = aes.CreateEncryptor();
                        using (CryptoStream cs = new CryptoStream(fsOutput, encryptor, CryptoStreamMode.Write))
                        {
                            int data;
                            while ((data = fsInput.ReadByte()) != -1)
                            {
                                cs.WriteByte((byte)data);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// رمزگشایی فایل با الگوریتم AES
        /// </summary>
        /// <param name="inputFile">مسیر فایل رمزنگاری شده</param>
        /// <param name="outputFile">مسیر فایل خروجی</param>
        public void DecryptFile(string inputFile, string outputFile)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = _key;
                aes.IV = _iv;

                using (FileStream fsInput = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
                {
                    using (FileStream fsOutput = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
                    {
                        ICryptoTransform decryptor = aes.CreateDecryptor();
                        using (CryptoStream cs = new CryptoStream(fsInput, decryptor, CryptoStreamMode.Read))
                        {
                            int data;
                            while ((data = cs.ReadByte()) != -1)
                            {
                                fsOutput.WriteByte((byte)data);
                            }
                        }
                    }
                }
            }
        }
    }
}
