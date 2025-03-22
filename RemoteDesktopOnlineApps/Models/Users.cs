using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RemoteDesktopOnlineApps.Models
{
    public class Users
    {
        public Users()
        {
            RemoteSessions = new HashSet<RemoteSession>();
            Notification = new HashSet<Notification>();
        }

        [Key]
        public int Id { get; set; }

        [DisplayName("نام و نام خانوادگی")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "لطفا {0} را وارد کنید")]
        [StringLength(150)]
        public string FullName { get; set; }

        [DataType(DataType.PhoneNumber)]
        [DisplayName("تلفن")]
        [StringLength(15)]
        public string Phone { get; set; }

        [DisplayName("کد ملی")]
        [StringLength(10)]
        public string NationalCode { get; set; }

        [DisplayName("نام کاربری")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "لطفا {0} را وارد کنید")]
        [StringLength(50)]
        public string UserName { get; set; }

        [DataType(DataType.EmailAddress)]
        [DisplayName("ایمیل")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "لطفا {0} را وارد کنید")]
        [StringLength(250)]
        public string Email { get; set; }

        [DataType(DataType.Password)]
        [DisplayName("کلمه عبور")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "لطفا {0} را وارد کنید")]
        [StringLength(550)]
        public string Password { get; set; }

        [DisplayName("کد فعالسازی")]
        public string ActiveCode { get; set; }

        [DisplayName("فعال/غیرفعال")]
        public bool? IsActive { get; set; }

        [DisplayName("تصویر کاربر")]
        [StringLength(1200)]
        [DataType(DataType.Upload)]
        public string Avatar { get; set; }

        [DisplayName("تاریخ ایجاد")]
        public DateTime RegisterDate { get; set; } = DateTime.Now;

        [DisplayName("شهرستان محل کار")]
        [StringLength(150)]
        public string CompanyLocationCity { get; set; }

        [DisplayName("نام دفتر/شعبه")]
        [StringLength(200)]
        public string CompanyOfficeName { get; set; }

        public RoleTitle RoleName { get; set; }

        // Navigation properties
        public virtual ICollection<RemoteSession> RemoteSessions { get; set; }

        public virtual ICollection<Notification> Notification { get; set; }
    }

    public enum RoleTitle
    {
        مدیر = 1,
        کاربر = 2,
        راهبر = 3
    }
}