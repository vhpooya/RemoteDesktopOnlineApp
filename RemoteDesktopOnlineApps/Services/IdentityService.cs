using Microsoft.IdentityModel.Tokens;
using RemoteDesktopOnlineApps.Helpers;
using RemoteDesktopOnlineApps.Models;
using System.Security.Claims;

namespace RemoteDesktopOnlineApps.Services
{
  
    public class IdentityService
    {
        public ClaimsIdentity CreateIdentity(Users user, string authenticationType)
        {
            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.UserName ?? ""),
            new Claim(ClaimTypes.Locality, user.CompanyOfficeName ?? ""),
            new Claim(ClaimTypes.StateOrProvince, user.CompanyLocationCity ?? ""),
            new Claim("UserId", user.Id.ToString()),
            new Claim(ClaimTypes.Surname, user.FullName ?? ""),
    
            new Claim("http://schemas.microsoft.com/accesscontrolservice/2010/07/claims/identityprovider", "ASP.NET Identity")
        };

            if (!string.IsNullOrEmpty(user.RoleName.GetDisplayFarsiName()))
            {
                claims.Add(new Claim(ClaimTypes.Role, user.RoleName.GetDisplayFarsiName()));
            }

            return new ClaimsIdentity(claims, authenticationType);
        }
    }
}
