using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RemoteDesktopOnlineApps.ViewModels;
using RemoteDesktopOnlineApps.Models;
using RemoteDesktopOnlineApps.Helpers;
using RemoteDesktopOnlineApps.Services;
using System.Net;
using System.Security.Claims;

namespace RemoteDesktopOnlineApps.Controllers
{
    public class AccountController : Controller
    {
        private readonly IdentityService identityService;
        private readonly ApplicationDbContext db;

        private readonly PasswordHasher<Users> _passwordHasher;

        public AccountController(ApplicationDbContext db, IdentityService identityService, PasswordHasher<Users> passwordHasher)
        {
            this.db = db;
            this.identityService = identityService;
            _passwordHasher = passwordHasher;
        }

        public IActionResult Index()
        {
            var result = db.Users.ToList();
            return View(result);
        }


        public IActionResult Register()
        {
            return View();
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(AccountRegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // چک کردن نام کاربری برای تکراری نبودن
                if (db.Users.Any(u => u.UserName == model.UserName.Trim().ToLower()))
                {
                    ModelState.AddModelError("UserName", "نام کاربری تکراری است");
                    return View(model);
                }

                var user = new Users
                {
                    UserName = model.UserName,
                    FullName = model.FullName,
                    NationalCode = model.NationalCode,
                    Email = model.EMail,
                    Phone = model.TellNum,
                    CompanyLocationCity = model.CompanyLocationCity,
                    CompanyOfficeName = model.CompanyOfficeName,
                    RoleName = model.RoleName,
                    IsActive = true,
                    RegisterDate = DateTime.Now,
                    ActiveCode = Guid.NewGuid().ToString(),
                    Avatar = "~/Images/avatar5.png", // مسیر پیش‌فرض برای آواتار
                };

                // بررسی تطابق پسوردها
                if (model.UserPassword != model.ReUserPassword)
                {
                    ModelState.AddModelError("ReUserPassword", "کلمه‌های عبور مغایرت دارند");
                    return View(model);
                }

                // هش کردن پسورد
                var passwordHasher = new PasswordHasher<Users>();
                user.Password = passwordHasher.HashPassword(user, model.UserPassword);

                // ذخیره کاربر در پایگاه داده
                db.Users.Add(user);
                await db.SaveChangesAsync();

                // هدایت به صفحه اصلی یا صفحه دیگری پس از ثبت‌نام
                return RedirectToAction("Index", "Users");
            }

            // در صورتی که مدل معتبر نبود، نمایش صفحه ثبت‌نام مجدداً
            return View(model);
        }


        //[Route("Login")]
        [AllowAnonymous]
        public IActionResult Login()
        {

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(UserLoginViewModel login, string returnUrl = "/Home/Index")
        {
            if (!ModelState.IsValid)
            {
                return View(login);
            }

            // ابتدا کاربر را فقط با نام کاربری پیدا می‌کنیم
            var user = db.Users.SingleOrDefault(u => u.UserName == login.UserName);
            if (user == null)
            {
                ModelState.AddModelError("UserName", "کاربری با این نام کاربری یافت نشد");
                return View(login);
            }

            if (!user.IsActive.HasValue || user.IsActive == false)
            {
                ModelState.AddModelError("UserName", "حساب کاربری شما فعال نشده است");
                return View(login);
            }

            // بررسی رمز عبور با PasswordHasher
           
            var result = _passwordHasher.VerifyHashedPassword(user, user.Password, login.Password);

            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("Password", "رمز عبور اشتباه است");
                return View(login);
            }

            var identity = identityService.CreateIdentity(user, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties
                {
                    IsPersistent = login.RememberMe,
                    ExpiresUtc = DateTime.UtcNow.AddHours(2)
                });
            return LocalRedirect(returnUrl);
        }

        // GET: User/EditProfile
        public IActionResult EditProfile(int? id)
        {
            if (id == null)
            {
                return BadRequest(); // معادل HttpStatusCode.BadRequest
            }

            var user = db.Users.Find(id);
            if (user == null)
            {
                return NotFound(); // معادل HttpNotFound
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(Users model, IFormFile imgUp)
        {
            if (ModelState.IsValid)
            {
                var loginUser = await db.Users.SingleOrDefaultAsync(x => x.UserName == User.Identity.Name);
                if (loginUser == null)
                {
                    return NotFound();
                }

                var userId = loginUser.Id;
                var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return NotFound();
                }

                // بررسی و ذخیره فایل تصویر
                if (imgUp != null && imgUp.Length > 0)
                {
                    // حذف تصویر قبلی اگر موجود باشد
                    if (!string.IsNullOrEmpty(user.Avatar))
                    {
                        var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Img", user.Avatar);
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    // ذخیره تصویر جدید
                    user.Avatar = Guid.NewGuid().ToString() + Path.GetExtension(imgUp.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Img", user.Avatar);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imgUp.CopyToAsync(stream);
                    }
                }

                // به‌روزرسانی اطلاعات کاربر
                user.Phone = model.Phone;
                user.Email = model.Email;
                user.FullName = model.FullName;
                user.NationalCode = model.NationalCode;
                user.CompanyLocationCity = model.CompanyLocationCity;
                user.RoleName = model.RoleName;
                user.CompanyOfficeName = model.CompanyOfficeName;

                // ذخیره تغییرات در پایگاه داده
                await db.SaveChangesAsync();

                // بازگشت به صفحه مشاهده پروفایل
                return RedirectToAction("Index", "Home");
            }

            return View(model);
        }


        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

    }
}
