using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.Data;
using CinemaBooking.Models;
using CinemaBooking.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CinemaBooking.Controllers
{
    [Authorize(Roles = "Admin")]
    public class LichChieuController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LichChieuController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: LichChieu
        public async Task<IActionResult> Index(LichChieuListViewModel viewModel = null)
        {
            if (viewModel == null)
            {
                viewModel = new LichChieuListViewModel();
            }

            // Lấy danh sách phim và rạp cho dropdown filter
            viewModel.PhimList = new SelectList(await _context.Phims.OrderBy(p => p.TenPhim).ToListAsync(), "MaPhim", "TenPhim");
            viewModel.RapList = new SelectList(await _context.RapPhims.OrderBy(r => r.TenRap).ToListAsync(), "MaRap", "TenRap");

            // Truy vấn cơ bản
            var query = _context.LichChieus
                .Include(l => l.Phim)
                .Include(l => l.PhongChieu)
                    .ThenInclude(p => p.RapPhim)
                .Include(l => l.NgonNguPhim)
                .AsQueryable();

            // Áp dụng các bộ lọc
            if (!string.IsNullOrEmpty(viewModel.SearchTerm))
            {
                query = query.Where(l => l.Phim.TenPhim.Contains(viewModel.SearchTerm) || 
                                        l.PhongChieu.RapPhim.TenRap.Contains(viewModel.SearchTerm));
            }

            if (viewModel.NgayBatDau.HasValue)
            {
                query = query.Where(l => l.NgayChieu >= viewModel.NgayBatDau.Value);
            }

            if (viewModel.NgayKetThuc.HasValue)
            {
                query = query.Where(l => l.NgayChieu <= viewModel.NgayKetThuc.Value);
            }

            if (viewModel.MaPhim.HasValue)
            {
                query = query.Where(l => l.MaPhim == viewModel.MaPhim.Value);
            }

            if (viewModel.MaRap.HasValue)
            {
                query = query.Where(l => l.PhongChieu.MaRap == viewModel.MaRap.Value);
            }

            // Sắp xếp
            query = query.OrderBy(l => l.NgayChieu).ThenBy(l => l.GioChieu);

            // Gán kết quả cho view model
            viewModel.LichChieus = await query.ToListAsync();

            return View(viewModel);
        }

        // GET: LichChieu/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lichChieu = await _context.LichChieus
                .Include(l => l.Phim)
                .Include(l => l.PhongChieu)
                    .ThenInclude(p => p.RapPhim)
                .Include(l => l.NgonNguPhim)
                .FirstOrDefaultAsync(m => m.MaLichChieu == id);

            if (lichChieu == null)
            {
                return NotFound();
            }

            return View(lichChieu);
        }

        // GET: LichChieu/Create
        public async Task<IActionResult> Create()
        {
            // Tạo mới ViewModel với giá trị mặc định
            var viewModel = new LichChieuViewModel
            {
                NgayChieu = DateTime.Today,
                GioChieu = new TimeSpan(9, 0, 0),
                GiaVe = 100000
            };
            
            // LoadRelatedData trước khi trả về view
            try
            {
                // Load phim
                viewModel.PhimList = new SelectList(
                    await _context.Phims.OrderBy(p => p.TenPhim).ToListAsync(), 
                    "MaPhim", 
                    "TenPhim"
                );
                
                // Load phòng chiếu với thông tin rạp
                var phongChieus = await _context.PhongChieus
                    .Include(p => p.RapPhim)
                    .OrderBy(p => p.RapPhim.TenRap)
                    .ThenBy(p => p.SoPhong)
                    .Select(p => new {
                        p.MaPhong,
                        TenPhong = $"Phòng {p.SoPhong} - {p.RapPhim.TenRap}"
                    })
                    .ToListAsync();
                    
                viewModel.PhongList = new SelectList(phongChieus, "MaPhong", "TenPhong");
                
                // Load ngôn ngữ phim
                var ngonNguList = await _context.NgonNguPhims
                    .OrderBy(n => n.NgonNgu)
                    .Select(n => new { 
                        n.MaNgonNgu, 
                        TenNgonNgu = $"{n.NgonNgu} ({n.PhuDe})" 
                    })
                    .ToListAsync();
                    
                viewModel.NgonNguList = new SelectList(ngonNguList, "MaNgonNgu", "TenNgonNgu");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi tải dữ liệu: {ex.Message}";
            }
            
            return View(viewModel);
        }
        
        // POST: LichChieu/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LichChieuViewModel viewModel)
        {
            try
            {
                // Kiểm tra lịch chiếu trùng
                var trungLich = await CheckTrungLich(viewModel);
                if (trungLich)
                {
                    ModelState.AddModelError("", "Lịch chiếu bị trùng với lịch chiếu khác trong cùng phòng");
                    await LoadRelatedData(viewModel);
                    return View(viewModel);
                }
                
                // Trực tiếp tạo lịch chiếu mới
                var lichChieu = new LichChieu
                {
                    MaPhim = viewModel.MaPhim,
                    MaPhong = viewModel.MaPhong,
                    NgayChieu = viewModel.NgayChieu,
                    GioChieu = viewModel.GioChieu,
                    GiaVe = viewModel.GiaVe
                };
                
                if (viewModel.MaNgonNgu.HasValue)
                {
                    lichChieu.MaNgonNgu = viewModel.MaNgonNgu;
                }
                
                _context.LichChieus.Add(lichChieu);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Tạo lịch chiếu mới thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi lưu lịch chiếu: {ex.Message}";
                
                // Tải lại dữ liệu cho dropdown
                await LoadRelatedData(viewModel);
                return View(viewModel);
            }
        }

        // GET: LichChieu/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lichChieu = await _context.LichChieus
                .Include(l => l.Phim)
                .Include(l => l.PhongChieu)
                .Include(l => l.NgonNguPhim)
                .FirstOrDefaultAsync(m => m.MaLichChieu == id);

            if (lichChieu == null)
            {
                return NotFound();
            }

            var viewModel = new LichChieuViewModel
            {
                MaLichChieu = lichChieu.MaLichChieu,
                MaPhim = lichChieu.MaPhim,
                MaPhong = lichChieu.MaPhong,
                NgayChieu = lichChieu.NgayChieu,
                GioChieu = lichChieu.GioChieu,
                GiaVe = lichChieu.GiaVe,
                MaNgonNgu = lichChieu.MaNgonNgu,
                TenPhim = lichChieu.Phim?.TenPhim,
                TenPhong = lichChieu.PhongChieu?.SoPhong.ToString(),
                TenRap = lichChieu.PhongChieu?.RapPhim?.TenRap,
                NgonNgu = lichChieu.NgonNguPhim?.NgonNgu
            };

            await LoadRelatedData(viewModel);
            return View(viewModel);
        }

        // POST: LichChieu/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, LichChieuViewModel viewModel)
        {
            if (id != viewModel.MaLichChieu)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Kiểm tra lịch chiếu trùng (không tính lịch hiện tại)
                    var trungLich = await CheckTrungLich(viewModel, id);
                    if (trungLich)
                    {
                        ModelState.AddModelError("", "Lịch chiếu bị trùng với lịch chiếu khác trong cùng phòng");
                        await LoadRelatedData(viewModel);
                        return View(viewModel);
                    }

                    // Cập nhật lịch chiếu
                    var lichChieu = await _context.LichChieus.FindAsync(id);
                    if (lichChieu == null)
                    {
                        return NotFound();
                    }

                    lichChieu.MaPhim = viewModel.MaPhim;
                    lichChieu.MaPhong = viewModel.MaPhong;
                    lichChieu.NgayChieu = viewModel.NgayChieu;
                    lichChieu.GioChieu = viewModel.GioChieu;
                    lichChieu.GiaVe = viewModel.GiaVe;
                    lichChieu.MaNgonNgu = viewModel.MaNgonNgu;

                    _context.Update(lichChieu);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật lịch chiếu thành công";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LichChieuExists(viewModel.MaLichChieu))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            
            await LoadRelatedData(viewModel);
            return View(viewModel);
        }

        // GET: LichChieu/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lichChieu = await _context.LichChieus
                .Include(l => l.Phim)
                .Include(l => l.PhongChieu)
                    .ThenInclude(p => p.RapPhim)
                .Include(l => l.NgonNguPhim)
                .FirstOrDefaultAsync(m => m.MaLichChieu == id);
                
            if (lichChieu == null)
            {
                return NotFound();
            }

            return View(lichChieu);
        }

        // POST: LichChieu/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var lichChieu = await _context.LichChieus.FindAsync(id);
            
            if (lichChieu == null)
            {
                return NotFound();
            }

            // Kiểm tra xem đã có đặt vé cho lịch chiếu này chưa
            var existingDatVe = await _context.DatVes.AnyAsync(d => d.MaLichChieu == id);
            if (existingDatVe)
            {
                TempData["ErrorMessage"] = "Không thể xóa lịch chiếu vì đã có người đặt vé cho suất chiếu này";
                return RedirectToAction(nameof(Index));
            }

            _context.LichChieus.Remove(lichChieu);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Xóa lịch chiếu thành công";
            return RedirectToAction(nameof(Index));
        }

        // Phương thức hỗ trợ load dữ liệu liên quan cho dropdown
        private async Task LoadRelatedData(LichChieuViewModel viewModel)
        {
            // Load phim
            viewModel.PhimList = new SelectList(
                await _context.Phims.OrderBy(p => p.TenPhim).ToListAsync(), 
                "MaPhim", 
                "TenPhim"
            );
            
            // Load phòng chiếu với thông tin rạp
            var phongChieus = await _context.PhongChieus
                .Include(p => p.RapPhim)
                .OrderBy(p => p.RapPhim.TenRap)
                .ThenBy(p => p.SoPhong)
                .Select(p => new {
                    p.MaPhong,
                    TenPhong = $"Phòng {p.SoPhong} - {p.RapPhim.TenRap}"
                })
                .ToListAsync();
                
            viewModel.PhongList = new SelectList(phongChieus, "MaPhong", "TenPhong");
            
            // Load ngôn ngữ phim
            var ngonNguList = await _context.NgonNguPhims
                .OrderBy(n => n.NgonNgu)
                .Select(n => new { 
                    n.MaNgonNgu, 
                    TenNgonNgu = $"{n.NgonNgu} ({n.PhuDe})" 
                })
                .ToListAsync();
                
            viewModel.NgonNguList = new SelectList(ngonNguList, "MaNgonNgu", "TenNgonNgu");
        }

        // Kiểm tra trùng lịch
        private async Task<bool> CheckTrungLich(LichChieuViewModel viewModel, int? excludeId = null)
        {
            // Lấy thông tin phim
            var phim = await _context.Phims.FindAsync(viewModel.MaPhim);
            if (phim == null) return false;

            // Tính thời gian kết thúc của lịch chiếu mới
            TimeSpan thoiLuongPhim = TimeSpan.FromMinutes(phim.ThoiLuong);
            DateTime thoiGianBatDau = viewModel.NgayChieu.Add(viewModel.GioChieu);
            DateTime thoiGianKetThuc = thoiGianBatDau.Add(thoiLuongPhim).AddMinutes(30); // Thêm 30 phút buffer

            // Tìm các lịch chiếu trùng
            var query = _context.LichChieus
                .Include(l => l.Phim)
                .Where(l => l.MaPhong == viewModel.MaPhong && l.NgayChieu == viewModel.NgayChieu);

            // Không kiểm tra với chính lịch hiện tại khi edit
            if (excludeId.HasValue)
            {
                query = query.Where(l => l.MaLichChieu != excludeId.Value);
            }

            var lichChieuTrung = await query.ToListAsync();

            // Kiểm tra từng lịch chiếu
            foreach (var lc in lichChieuTrung)
            {
                TimeSpan thoiLuongLC = TimeSpan.FromMinutes(lc.Phim.ThoiLuong);
                DateTime lcBatDau = lc.NgayChieu.Add(lc.GioChieu);
                DateTime lcKetThuc = lcBatDau.Add(thoiLuongLC).AddMinutes(30); // Thêm 30 phút buffer

                // Trùng lịch nếu:
                // 1. Thời gian bắt đầu nằm trong khoảng thời gian của lịch chiếu khác
                // 2. Thời gian kết thúc nằm trong khoảng thời gian của lịch chiếu khác
                // 3. Thời gian bắt đầu trước và kết thúc sau lịch chiếu khác
                if ((thoiGianBatDau >= lcBatDau && thoiGianBatDau < lcKetThuc) ||
                    (thoiGianKetThuc > lcBatDau && thoiGianKetThuc <= lcKetThuc) ||
                    (thoiGianBatDau <= lcBatDau && thoiGianKetThuc >= lcKetThuc))
                {
                    return true; // Trùng lịch
                }
            }

            return false; // Không trùng lịch
        }

        private bool LichChieuExists(int id)
        {
            return _context.LichChieus.Any(e => e.MaLichChieu == id);
        }
    }
} 