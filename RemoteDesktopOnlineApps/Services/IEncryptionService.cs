namespace RemoteDesktopOnlineApps.Services
{
    /// <summary>
    /// سرویس رمزنگاری و رمزگشایی داده‌ها
    /// </summary>
    public interface IEncryptionService
    {
        /// <summary>
        /// رمزنگاری رمز عبور
        /// </summary>
        /// <param name="password">رمز عبور اصلی</param>
        /// <returns>رمز عبور رمزنگاری شده</returns>
        string EncryptPassword(string password);

        /// <summary>
        /// رمزگشایی رمز عبور
        /// </summary>
        /// <param name="encryptedPassword">رمز عبور رمزنگاری شده</param>
        /// <returns>رمز عبور اصلی</returns>
        string DecryptPassword(string encryptedPassword);

        /// <summary>
        /// رمزنگاری داده
        /// </summary>
        /// <param name="data">داده اصلی</param>
        /// <returns>داده رمزنگاری شده</returns>
        byte[] EncryptData(byte[] data);

        /// <summary>
        /// رمزگشایی داده
        /// </summary>
        /// <param name="encryptedData">داده رمزنگاری شده</param>
        /// <returns>داده اصلی</returns>
        byte[] DecryptData(byte[] encryptedData);

        /// <summary>
        /// تولید کلید رمزنگاری
        /// </summary>
        /// <returns>کلید رمزنگاری</returns>
        string GenerateEncryptionKey();

        /// <summary>
        /// هش کردن داده
        /// </summary>
        /// <param name="data">داده اصلی</param>
        /// <returns>هش داده</returns>
        string ComputeHash(string data);

        /// <summary>
        /// بررسی صحت هش
        /// </summary>
        /// <param name="data">داده اصلی</param>
        /// <param name="hash">هش ذخیره شده</param>
        /// <returns>نتیجه بررسی</returns>
        bool VerifyHash(string data, string hash);
    }
}