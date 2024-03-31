namespace WebApp.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class tb_Booking
    {
        public int ID { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        [Required]
        [StringLength(100)]
        public string Status { get; set; }

        public string Note { get; set; }

        public int? UserID { get; set; }

        public int? ArenaID { get; set; }

        public virtual tb_Arena tb_Arena { get; set; }

        public virtual tb_User tb_User { get; set; }
    }
}
