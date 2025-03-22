using RemoteDesktopOnlineApps.Models;
using RemoteDesktopOnlineApps.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RemoteDesktopOnlineApps.ViewModels
{
    public class AccountRegisterViewModel
    {

        [Display(Name = "نام کامل")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        public string FullName { get; set; }

        [DisplayName("کد ملی")]
        [StringLength(10)]
        public string NationalCode { get; set; }

        [DisplayName("نام کاربری")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "لطفا {0} را وارد کنید")]
        [StringLength(50)]
        public string UserName { get; set; }

        [Display(Name = "کلمه عبور")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        [DataType(DataType.Password)]
        public string UserPassword { get; set; }

        [Display(Name = "تکرار کلمه عبور")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        [DataType(DataType.Password)]
        [Compare("UserPassword", ErrorMessage = "کلمه های عبور مغایرت دارند")]
        public string ReUserPassword { get; set; }


        [Display(Name = "تلفن")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        public string TellNum { get; set; }

        [Display(Name = "ایمیل")]
        [DataType(DataType.EmailAddress)]
        [EmailAddress(ErrorMessage = "ایمیل وارد شده معتبر نیست")]
        public string EMail { get; set; }


        [DisplayName("شهرستان محل کار")]
        [StringLength(150)]
        public string CompanyLocationCity { get; set; }

        [DisplayName("نام دفتر/شعبه")]
        [StringLength(200)]
        public string CompanyOfficeName { get; set; }

        [Display(Name = "نقش کاربر")]
        public RoleTitle RoleName { get; set; }
    }

    public class AccountProfile
    {
        public long UserID { get; set; }
       
        [DisplayName("نوع دسترسی")]
        public long? RoleID { get; set; }

        [Display(Name = "نام")]

        public string FirstName { get; set; }

        [Display(Name = "نام خانوادگی")]

        public string LastName { get; set; }

        [Display(Name = "کد ملی")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        public string NationalCode { get; set; }

        [Display(Name = "تلفن")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        public string TellNum { get; set; }

        [Display(Name = "ایمیل")]
        [DataType(DataType.EmailAddress)]
        public string EMail { get; set; }

        [DisplayName("نام کاربری")]

        public string UserName { get; set; }

        [Display(Name = "رمز عبور قدیمی")]
        [DataType(DataType.Password)]
        public string OldPassword { get; set; } // در صورت نیاز به نمایش رمز عبور قدیمی، این فیلد قابل استفاده است

        [Display(Name = "رمز عبور جدید")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } // رمز عبور جدید

        [Display(Name = "تکرار رمز عبور جدید")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "کلمه‌های عبور مغایرت دارند")]
        public string ReNewPassword { get; set; } // تکرار رمز عبور جدید

        [DisplayName("شهرستان محل کار")]
        [StringLength(150)]
        public string CompanyLocationCity { get; set; }

        [DisplayName("نام دفتر/شعبه")]
        [StringLength(200)]
        public string CompanyOfficeName { get; set; }



        [DisplayName("تصویر کاربر")]
        [StringLength(1200)]
        [DataType(DataType.Upload)]
        public string Avatar { get; set; }

    }

    public class UserLoginViewModel
    {

        [Display(Name = "نام کاربری")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        public string UserName { get; set; }

        [Display(Name = "کلمه عبور")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "مرا به خاطر بسپار")]
        public bool RememberMe { get; set; }
    }

    public class ForgotPasswordViewModel
    {
        [Display(Name = "ایمیل")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        [EmailAddress(ErrorMessage = "ایمیل وارد شده معتبر نمی باشد")]
        public string Email { get; set; }
    }

    public class RecoveryPasswordViewModel
    {
        [Display(Name = "کلمه عبور جدید")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "تکرار کلمه عبور جدید")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "کلمه های عبور مغایرت دارند")]
        public string RePassword { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Display(Name = "کلمه عبور فعلی")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        [DataType(DataType.Password)]
        public string OldPassword { get; set; }

        [Display(Name = "کلمه عبور جدید")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "تکرار کلمه عبور جدید")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "کلمه های عبور مغایرت دارند")]
        public string RePassword { get; set; }
    }

    public class UserExternalLoginConfirmationViewModel
    {
        [Required]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }

    public class UserExternalLoginListViewModel
    {
        public string ReturnUrl { get; set; }
    }

    //public class UserSendCodeViewModel
    //{
    //    public string SelectedProvider { get; set; }
    //    public ICollection<System.Web.Mvc.SelectListItem> Providers { get; set; }
    //    public string ReturnUrl { get; set; }
    //    public bool RememberMe { get; set; }
    //}

    public class UserVerifyCodeViewModel
    {
        [Required]
        public string Provider { get; set; }

        [Required]
        [Display(Name = "Code")]
        public string Code { get; set; }
        public string ReturnUrl { get; set; }

        [Display(Name = "Remember this browser?")]
        public bool RememberBrowser { get; set; }

        public bool RememberMe { get; set; }
    }

}