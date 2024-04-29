namespace WebApp.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class tb_User
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public tb_User()
        {
            tb_Booking = new HashSet<tb_Booking>();
        }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Key]
        [StringLength(10)]
        public string UserID { get; set; }

        [Required]
        [StringLength(100)]
        [DisplayName("Tên tài khoản")]

        public string UserName { get; set; }

        [Required]
        [StringLength(100)]
        [DisplayName("Mật khẩu")]

        public string Password { get; set; }

        [StringLength(255)]
        [DisplayName("Họ và tên")]

        public string FullName { get; set; }

        [StringLength(255)]
        [DisplayName("Email")]

        public string Email { get; set; }

        [StringLength(20)]
        [DisplayName("Số điện thoại")]

        public string Phone { get; set; }

        [StringLength(255)]
        [DisplayName("Địa chỉ")]

        public string Address { get; set; }

        public int? Status { get; set; }

        [StringLength(10)]
        public string RoleID { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<tb_Booking> tb_Booking { get; set; }

        public virtual tb_Role tb_Role { get; set; }
    }
}
