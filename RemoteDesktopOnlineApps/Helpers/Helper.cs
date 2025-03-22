using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace RemoteDesktopOnlineApps.Helpers
{
    public static class AppHelpers
    {
        public static string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                // تبدیل رمز عبور به بایت‌ها
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

                // محاسبه hash با استفاده از SHA-256
                byte[] hashedBytes = sha256.ComputeHash(passwordBytes);

                // تبدیل hash به رشته هگزادسیمال
                string hashedPassword = BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();

                return hashedPassword;
            }
        }

        public static string GetDisplayFarsiName(this Enum enumValue)
        {
            // بررسی null بودن enumValue
            if (enumValue == null)
                return string.Empty;

            try
            {
                // دریافت FieldInfo
                var fieldInfo = enumValue.GetType().GetField(enumValue.ToString());
                if (fieldInfo == null)
                    return enumValue.ToString();

                // دریافت DisplayAttribute ها
                var attributes = fieldInfo.GetCustomAttributes(typeof(DisplayAttribute), false);
                if (attributes == null || attributes.Length == 0)
                    return enumValue.ToString();

                // بررسی و تبدیل به DisplayAttribute
                var displayAttr = attributes[0] as DisplayAttribute;
                if (displayAttr == null || string.IsNullOrEmpty(displayAttr.Name))
                    return enumValue.ToString();

                return displayAttr.Name;
            }
            catch
            {
                // در صورت هر خطایی، خود مقدار enum را برگردان
                return enumValue.ToString();
            }
        }



    }



    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// شناسه کاربر لاگین شده
        /// </summary>
        /// <param name="principal"></param>
        /// <returns></returns>
        public static int GetUserId(this ClaimsPrincipal principal)
        {
            var userId = principal.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
            return !string.IsNullOrEmpty(userId) ? int.Parse(userId) : 0;
        }


        /// <summary>
        /// دریافت UserName (از ClaimTypes.Name)
        /// </summary>
        /// <param name="principal"></param>
        /// <returns></returns>
        public static string GetUserName(this ClaimsPrincipal principal)
        {
            return principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? string.Empty;
        }

        /// <summary>
        /// دریافت FullName (از ClaimTypes.Surname)
        /// </summary>
        /// <param name="principal"></param>
        /// <returns></returns>
        public static string GetFullName(this ClaimsPrincipal principal)
        {
            return principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value ?? string.Empty;
        }

        /// <summary>
        /// دریافت RoleName (از ClaimTypes.Role)
        /// </summary>
        /// <param name="principal"></param>
        /// <returns></returns>
        public static string GetRole(this ClaimsPrincipal principal)
        {
            return principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? string.Empty;
        }

        /// <summary>
        /// دریافت CompanyOfficeName (از ClaimTypes.Locality)
        /// </summary>
        /// <param name="principal"></param>
        /// <returns></returns>
        public static string GetCompanyOfficeName(this ClaimsPrincipal principal)
        {
            return principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Locality)?.Value ?? string.Empty;
        }

        /// <summary>
        /// دریافت CompanyLocationCity (از ClaimTypes.StateOrProvince)
        /// </summary>
        /// <param name="principal"></param>
        /// <returns></returns>
        public static string GetCompanyLocationCity(this ClaimsPrincipal principal)
        {
            return principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.StateOrProvince)?.Value ?? string.Empty;
        }

        /// <summary>
        /// متد کمکی برای بررسی اینکه آیا کاربر نقش خاصی دارد یا نه
        /// </summary>
        /// <param name="principal"></param>
        /// <param name="roleName"></param>
        /// <returns></returns>
        public static bool IsInRole(this ClaimsPrincipal principal, string roleName)
        {
            return principal.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == roleName);
        }
    }

}
