using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;

namespace RemoteDesktopOnlineApps.Models
{
    public class ApplicationDbContext : DbContext
    {
        /// <summary>
        /// سازنده کلاس کانتکست
        /// </summary>
        /// <param name="options">گزینه‌های کانتکست</param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// جلسات ریموت
        /// </summary>
        public virtual DbSet<RemoteSession> RemoteSessions { get; set; }

        /// <summary>
        /// انتقال فایل‌ها
        /// </summary>
        public virtual DbSet<FileTransfer> FileTransfers { get; set; }

        /// <summary>
        /// پیام‌های چت
        /// </summary>
        public virtual DbSet<ChatMessage> ChatMessages { get; set; }

        /// <summary>
        /// شرکت‌کنندگان در جلسه
        /// </summary>
        public virtual DbSet<SessionParticipant> SessionParticipants { get; set; }

        /// <summary>
        /// اعلان‌ها
        /// </summary>
        public virtual DbSet<Notification> Notifications { get; set; }

        /// <summary>
        /// اعضای گروه
        /// </summary>
        public virtual DbSet<GroupMember> GroupMembers { get; set; }

        /// <summary>
        /// دسترسی به سرور
        /// </summary>
        public virtual DbSet<ServerAccess> ServerAccess { get; set; }

        /// <summary>
        /// اطلاعات سرور
        /// </summary>
        public virtual DbSet<ServerInfo> ServerInfos { get; set; }

        public virtual DbSet<Users> Users { get; set; }

        public DbSet<RemoteConnectionStats> ConnectionStats { get; set; }

        public DbSet<ClientConnectionInfo> ClientConnectionInfo { get; set; }

        public DbSet<ClientRegistration> ClientRegistrations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<GroupMember>().ToTable("GroupMembers");


            // تنظیم ارتباطات بین جداول

            // ارتباط کاربر با جلسات ریموت
            modelBuilder.Entity<RemoteSession>()
                .HasOne(s => s.User)
                .WithMany(u => u.RemoteSessions)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ارتباط بین جلسه‌های ریموت و شرکت‌کنندگان
            modelBuilder.Entity<SessionParticipant>()
                .HasOne(p => p.RemoteSession)
                .WithMany(s => s.Participants)
                .HasForeignKey(p => p.RemoteSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SessionParticipant>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ارتباط بین جلسه‌های ریموت و پیام‌های چت
            modelBuilder.Entity<ChatMessage>()
                .HasOne(m => m.RemoteSession)
                .WithMany(s => s.ChatMessages)
                .HasForeignKey(m => m.RemoteSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            // ارتباط بین جلسه‌های ریموت و انتقال فایل
            modelBuilder.Entity<FileTransfer>()
                .HasOne(f => f.RemoteSession)
                .WithMany(s => s.FileTransfers)
                .HasForeignKey(f => f.RemoteSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // ارتباط بین کاربر و انتقال فایل
            modelBuilder.Entity<FileTransfer>()
      .HasOne(f => f.User)
      .WithMany()
      .HasForeignKey(f => f.UserId)
      .OnDelete(DeleteBehavior.Restrict)
      .IsRequired(false);

            // ارتباط بین آمار اتصال و جلسه ریموت
            modelBuilder.Entity<RemoteConnectionStats>()
                .HasOne<RemoteSession>()
                .WithMany()
                .HasForeignKey(s => s.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // ارتباط بین کاربر و اعلان‌ها
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notification)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ارتباط بین کاربر و دسترسی به سرور
            modelBuilder.Entity<ServerAccess>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ارتباط بین کاربر و عضویت در گروه
            modelBuilder.Entity<GroupMember>()
                .HasOne(g => g.User)
                .WithMany()
                .HasForeignKey(g => g.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}


