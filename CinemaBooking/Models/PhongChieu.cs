using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace CinemaBooking.Models
{
    [Table("phong_chieu")]
    public class PhongChieu
    {
        [Key]
        [Column("ma_phong")]
        public int MaPhong { get; set; }

        [Required]
        [Column("ma_rap")]
        public int MaRap { get; set; }

        [Required]
        [Column("so_phong")]
<<<<<<< HEAD
        public int SoPhong { get; set; }
=======
        public string SoPhong { get; set; }
>>>>>>> origin/feature/nguyentraduydat

        [Required]
        [Column("suc_chua")]
        public int SucChua { get; set; }

<<<<<<< HEAD
=======
        [Column("loai_phong")]
        [StringLength(50)]
        public string? LoaiPhong { get; set; }

>>>>>>> origin/feature/nguyentraduydat
        [ForeignKey("MaRap")]
        public virtual RapPhim RapPhim { get; set; }

        public virtual ICollection<LichChieu> LichChieus { get; set; }
        public virtual ICollection<Ghe> Ghes { get; set; }
        
        [NotMapped]
        public string TenPhong => $"Phòng {SoPhong}";
    }
} 