using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.Data;
using CinemaBooking.Models;
using Microsoft.AspNetCore.Authorization;

namespace CinemaBooking.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminBookingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminBookingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: AdminBooking
        public async Task<IActionResult> Index(int? rapId, int? phongId, string trangThai, DateTime? tuNgay, DateTime? denNgay)
        {
            // Lấy danh sách rạp phim cho dropdown
            ViewBag.RapPhims = await _context.RapPhims.ToListAsync();
            
            // Lấy danh sách phòng chiếu (nếu có chọn rạp)
            if (rapId.HasValue)
            {
                ViewBag.PhongChieus = await _context.PhongChieus
                    .Where(p => p.MaRap == rapId.Value)
                    .ToListAsync();
            }
            
            // Danh sách các trạng thái đặt vé
            ViewBag.TrangThais = new List<string> { "Đã đặt", "Đã thanh toán", "Đã hủy", "Chờ thanh toán" };
            
            // Lấy query danh sách đặt vé
            var datVeQuery = _context.DatVes
                .Include(d => d.NguoiDung)
                .Include(d => d.LichChieu)
                    .ThenInclude(l => l.Phim)
                .Include(d => d.LichChieu)
                    .ThenInclude(l => l.PhongChieu)
                        .ThenInclude(p => p.RapPhim)
                .Include(d => d.KhuyenMai)
                .Include(d => d.DatVeGhes)
                    .ThenInclude(dvg => dvg.Ghe)
                .AsQueryable();

            // Lọc theo rạp phim (nếu có)
            if (rapId.HasValue)
            {
                datVeQuery = datVeQuery.Where(d => d.LichChieu.PhongChieu.MaRap == rapId.Value);
            }
            
            // Lọc theo phòng chiếu (nếu có)
            if (phongId.HasValue)
            {
                datVeQuery = datVeQuery.Where(d => d.LichChieu.MaPhong == phongId.Value);
            }
            
            // Lọc theo trạng thái (nếu có)
            if (!string.IsNullOrEmpty(trangThai))
            {
                datVeQuery = datVeQuery.Where(d => d.TrangThai == trangThai);
            }
            
            // Lọc theo ngày đặt (nếu có)
            if (tuNgay.HasValue)
            {
                datVeQuery = datVeQuery.Where(d => d.NgayDat >= tuNgay.Value.Date);
            }
            
            if (denNgay.HasValue)
            {
                datVeQuery = datVeQuery.Where(d => d.NgayDat <= denNgay.Value.Date.AddDays(1).AddSeconds(-1));
            }

            // Sắp xếp theo ngày đặt mới nhất
            datVeQuery = datVeQuery.OrderByDescending(d => d.NgayDat);
            
            // Lưu giá trị lọc vào ViewBag để hiển thị lại trên form
            ViewBag.RapId = rapId;
            ViewBag.PhongId = phongId;
            ViewBag.TrangThai = trangThai;
            ViewBag.TuNgay = tuNgay?.ToString("yyyy-MM-dd");
            ViewBag.DenNgay = denNgay?.ToString("yyyy-MM-dd");

            return View(await datVeQuery.ToListAsync());
        }

        // GET: AdminBooking/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var datVe = await _context.DatVes
                .Include(d => d.NguoiDung)
                .Include(d => d.LichChieu)
                    .ThenInclude(l => l.Phim)
                .Include(d => d.LichChieu)
                    .ThenInclude(l => l.PhongChieu)
                        .ThenInclude(p => p.RapPhim)
                .Include(d => d.KhuyenMai)
                .Include(d => d.DatVeGhes)
                    .ThenInclude(dvg => dvg.Ghe)
                .Include(d => d.ThanhToans)
                .FirstOrDefaultAsync(m => m.MaDatVe == id);
                
            if (datVe == null)
            {
                return NotFound();
            }

            return View(datVe);
        }

        // POST: AdminBooking/CapNhatTrangThai/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhatTrangThai(int id, string trangThai)
        {
            var datVe = await _context.DatVes
                .Include(d => d.DatVeGhes)
                .FirstOrDefaultAsync(d => d.MaDatVe == id);
                
            if (datVe == null)
            {
                return NotFound();
            }

            // Lưu trạng thái cũ trước khi cập nhật
            string trangThaiCu = datVe.TrangThai;
            
            // Cập nhật trạng thái mới
            datVe.TrangThai = trangThai;
            
            // Nếu cập nhật trạng thái thành "Đã hủy" và trạng thái trước đó không phải là "Đã hủy"
            if (trangThai == "Đã hủy" && trangThaiCu != "Đã hủy")
            {
                // Xóa các bản ghi trong bảng dat_ve_ghe để mở lại ghế
                if (datVe.DatVeGhes != null && datVe.DatVeGhes.Any())
                {
                    _context.DatVeGhes.RemoveRange(datVe.DatVeGhes);
                    
                    // Ghi log thông tin hủy vé
                    string thongTinGhe = string.Join(", ", datVe.DatVeGhes.Select(dvg => dvg.Ghe?.SoGhe));
                    var lichSu = new LichSuGiaoDich
                    {
                        MaNguoiDung = datVe.MaNguoiDung,
                        LoaiGiaoDich = "Hủy vé",
                        TrangThai = "Thành công",
                        NoiDung = $"Admin hủy vé #{datVe.MaDatVe}, mở lại ghế: {thongTinGhe}",
                        NgayGiaoDich = DateTime.Now
                    };
                    
                    _context.LichSuGiaoDiches.Add(lichSu);
                    
                    TempData["SuccessMessage"] = "Cập nhật trạng thái vé thành công! Các ghế đã được mở lại.";
                }
                else
                {
                    TempData["SuccessMessage"] = "Cập nhật trạng thái vé thành công!";
                }
            }
            else
            {
                TempData["SuccessMessage"] = "Cập nhật trạng thái vé thành công!";
            }
            
            await _context.SaveChangesAsync();
            
            return RedirectToAction(nameof(Details), new { id = id });
        }

        // Ajax endpoint to get rooms by cinema
        [HttpGet]
        public async Task<JsonResult> GetPhongsByRap(int rapId)
        {
            var phongChieus = await _context.PhongChieus
                .Where(p => p.MaRap == rapId)
                .Select(p => new { 
                    MaPhong = p.MaPhong, 
                    TenPhong = $"Phòng {p.SoPhong}" 
                })
                .ToListAsync();
                
            return Json(phongChieus);
        }
    }
} 