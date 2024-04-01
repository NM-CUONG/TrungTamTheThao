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

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public int Status { get; set; }

        public string Note { get; set; }

        [StringLength(10)]
        public string UserID { get; set; }

        [StringLength(10)]
        public string ArenaID { get; set; }

        public virtual tb_Arena tb_Arena { get; set; }

        public virtual tb_User tb_User { get; set; }
    }
}
