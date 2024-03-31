using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;

namespace WebApp.Models
{
    public partial class TrungTamTheThaoContext : DbContext
    {
        public TrungTamTheThaoContext()
            : base("name=TrungTamTheThaoContext")
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
                .HasMany(e => e.tb_Booking)
                .WithOptional(e => e.tb_Arena)
                .HasForeignKey(e => e.ArenaID);

            modelBuilder.Entity<tb_Category>()
                .HasMany(e => e.tb_Arena)
                .WithOptional(e => e.tb_Category)
                .HasForeignKey(e => e.CateID);

            modelBuilder.Entity<tb_Role>()
                .HasMany(e => e.tb_User)
                .WithOptional(e => e.tb_Role)
                .HasForeignKey(e => e.RoleID);

            modelBuilder.Entity<tb_Size>()
                .HasMany(e => e.tb_Arena)
                .WithOptional(e => e.tb_Size)
                .HasForeignKey(e => e.SizeID);

            modelBuilder.Entity<tb_User>()
                .HasMany(e => e.tb_Booking)
                .WithOptional(e => e.tb_User)
                .HasForeignKey(e => e.UserID);
        }
    }
}
