using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;

namespace WebApp.Models
{
    public partial class DBContext : DbContext
    {
        public DBContext()
            : base("name=DBContext")
        {
        }

        public virtual DbSet<tb_Arena> tb_Arena { get; set; }
        public virtual DbSet<tb_Booking> tb_Booking { get; set; }
        public virtual DbSet<tb_Category> tb_Category { get; set; }
        public virtual DbSet<tb_Role> tb_Role { get; set; }
        public virtual DbSet<tb_Size> tb_Size { get; set; }
        public virtual DbSet<tb_User> tb_User { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<tb_Arena>()
                .Property(e => e.ArenaID)
                .IsUnicode(false);

            modelBuilder.Entity<tb_Arena>()
                .Property(e => e.CateID)
                .IsUnicode(false);

            modelBuilder.Entity<tb_Arena>()
                .Property(e => e.SizeID)
                .IsUnicode(false);

            modelBuilder.Entity<tb_Booking>()
                .Property(e => e.BookingID)
                .IsUnicode(false);

            modelBuilder.Entity<tb_Booking>()
                .Property(e => e.UserID)
                .IsUnicode(false);

            modelBuilder.Entity<tb_Booking>()
                .Property(e => e.ArenaID)
                .IsUnicode(false);

            modelBuilder.Entity<tb_Category>()
                .Property(e => e.CateID)
                .IsUnicode(false);

            modelBuilder.Entity<tb_Role>()
                .Property(e => e.RoleID)
                .IsUnicode(false);

            modelBuilder.Entity<tb_Size>()
                .Property(e => e.SizeID)
                .IsUnicode(false);

            modelBuilder.Entity<tb_User>()
                .Property(e => e.UserID)
                .IsUnicode(false);

            modelBuilder.Entity<tb_User>()
                .Property(e => e.RoleID)
                .IsUnicode(false);
        }
    }
}
