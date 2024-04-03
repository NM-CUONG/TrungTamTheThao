namespace WebApp.Models
{
    using System;
    using System.Collections.Generic;
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
        [Display(Name = "Mã người dùng")]
        [Required(ErrorMessage = "Vui lòng nhập trường thông tin này!")]
        public string UserID { get; set; }

        [StringLength(100)]
        [Display(Name = "Tên đăng nhập")]
        [Required(ErrorMessage = "Vui lòng nhập trường thông tin này!")]
        public string UserName { get; set; }

        [Display(Name = "Mật khẩu")]
        [Required(ErrorMessage = "Vui lòng nhập trường thông tin này!")]
        [StringLength(100)]
        public string Password { get; set; }

        [StringLength(255)]

        [Display(Name = "Họ và tên")]

        public string FullName { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập trường thông tin này!")]
        [StringLength(255)]
        [Display(Name = "Email")]
        
        public string Email { get; set; }

        [StringLength(20)]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; }

        [StringLength(255)]
        [Display(Name = "Địa chỉ")]

        public string Address { get; set; }

        [StringLength(10)]
        [Display(Name = "Vai trò")]

        public string RoleID { get; set; }

        public int Status { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<tb_Booking> tb_Booking { get; set; }

        public virtual tb_Role tb_Role { get; set; }
    }
}
