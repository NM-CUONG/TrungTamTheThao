namespace WebApp.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class tb_Shift
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public tb_Shift()
        {
            tb_Booking = new HashSet<tb_Booking>();
        }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Key]
        [StringLength(10)]
        public string ShiftID { get; set; }

        [Required]
        [StringLength(20)]
        public string ShiftName { get; set; }

        public decimal? Price { get; set; }

        [StringLength(10)]
        public string CateID { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<tb_Booking> tb_Booking { get; set; }

        public virtual tb_Category tb_Category { get; set; }
    }
}
