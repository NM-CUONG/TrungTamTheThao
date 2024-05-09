
namespace WebApp.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class tb_Arena
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public tb_Arena()
        {
            tb_Booking = new HashSet<tb_Booking>();
        }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Key]
        [StringLength(10)]
        public string ArenaID { get; set; }

        [Required]
        [StringLength(255)]
        public string ArenaName { get; set; }

        [StringLength(255)]
        public string Image { get; set; }

        [StringLength(10)]
        public string CateID { get; set; }

        [StringLength(10)]
        public string SizeID { get; set; }
        [NotMapped]
        public string SizeName { get; set; }

        [NotMapped]
        public string CateName { get; set; }

        public string Description { get; set; }

        public int? MaxPersons { get; set; }

        public virtual tb_Category tb_Category { get; set; }

        public virtual tb_Size tb_Size { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<tb_Booking> tb_Booking { get; set; }
    }
}
