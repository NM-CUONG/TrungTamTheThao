namespace WebApp.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class tb_Booking
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Key]
        [StringLength(10)]
        public string BookingID { get; set; }

        [StringLength(10)]
        public string UserID { get; set; }

        [StringLength(10)]
        public string ArenaID { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        [StringLength(10)]
        public string ShiftID { get; set; }

        public string Note { get; set; }

        public int? Status { get; set; }

        [StringLength(20)]
        public string PhoneNumber { get; set; }

        [StringLength(100)]
        public string ContactName { get; set; }

        [NotMapped]
        public DateTime ngaySuDung { get; set; }

        [NotMapped]
        public int isCoDinh { get; set; }

        public double? Money { get; set; }

        public virtual tb_Arena tb_Arena { get; set; }

        public virtual tb_Shift tb_Shift { get; set; }

        public virtual tb_User tb_User { get; set; }
    }
}
