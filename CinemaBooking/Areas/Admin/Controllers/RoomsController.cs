using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.Data;
using CinemaBooking.Models;
using System.Text.Json;

namespace CinemaBooking.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class RoomsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RoomsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Rooms/Index?cinemaId=5
        public async Task<IActionResult> Index(int cinemaId)
        {
            var cinema = await _context.RapPhims
                .Include(r => r.PhongChieus)
                .FirstOrDefaultAsync(r => r.MaRap == cinemaId);

            if (cinema == null) return NotFound();

            ViewBag.CinemaName = cinema.TenRap;
            ViewBag.CinemaId = cinemaId;
            return View(cinema.PhongChieus);
        }

        // GET: Admin/Rooms/Create?cinemaId=5
        public IActionResult Create(int cinemaId)
        {
            ViewBag.CinemaId = cinemaId;
            return View(new PhongChieu { MaRap = cinemaId, SoHang = 10, SoCot = 10 });
        }

        // POST: Admin/Rooms/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PhongChieu room)
        {
            if (ModelState.IsValid)
            {
                _context.Add(room);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm phòng chiếu thành công! Hãy thiết kế sơ đồ ghế.";
                return RedirectToAction(nameof(DesignSeats), new { id = room.MaPhong });
            }
            ViewBag.CinemaId = room.MaRap;
            return View(room);
        }

        // GET: Admin/Rooms/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var room = await _context.PhongChieus.FindAsync(id);
            if (room == null) return NotFound();
            return View(room);
        }

        // POST: Admin/Rooms/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PhongChieu room)
        {
            if (id != room.MaPhong) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(room);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật thông tin phòng thành công!";
                    return RedirectToAction(nameof(Index), new { cinemaId = room.MaRap });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RoomExists(room.MaPhong)) return NotFound();
                    else throw;
                }
            }
            return View(room);
        }

        // GET: Admin/Rooms/DesignSeats/5
        public async Task<IActionResult> DesignSeats(int id)
        {
            var room = await _context.PhongChieus
                .Include(p => p.Ghes)
                .Include(p => p.RapPhim)
                .FirstOrDefaultAsync(p => p.MaPhong == id);

            if (room == null) return NotFound();

            return View(room);
        }

        // POST: Admin/Rooms/SaveLayout
        [HttpPost]
        public async Task<IActionResult> SaveLayout([FromBody] SeatLayoutDto layout)
        {
            if (layout == null) return Json(new { success = false, message = "Dữ liệu không hợp lệ." });

            var room = await _context.PhongChieus
                .Include(p => p.Ghes)
                .FirstOrDefaultAsync(p => p.MaPhong == layout.RoomId);

            if (room == null) return Json(new { success = false, message = "Không tìm thấy phòng." });

            // Update room dimensions
            room.SoHang = layout.Rows;
            room.SoCot = layout.Cols;
            room.SucChua = layout.Seats.Count(s => s.SeatType != "Empty");

            // Remove old seats
            _context.Ghes.RemoveRange(room.Ghes);

            // Add new seats
            foreach (var s in layout.Seats)
            {
                if (s.SeatType == "Empty") continue;

                _context.Ghes.Add(new Ghe
                {
                    MaPhong = room.MaPhong,
                    SoGhe = s.SeatName,
                    LoaiGhe = s.SeatType,
                    Hang = s.Row,
                    Cot = s.Col
                });
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Lưu sơ đồ ghế thành công!" });
        }

        private bool RoomExists(int id)
        {
            return _context.PhongChieus.Any(e => e.MaPhong == id);
        }
    }

    public class SeatLayoutDto
    {
        public int RoomId { get; set; }
        public int Rows { get; set; }
        public int Cols { get; set; }
        public List<SeatDto> Seats { get; set; }
    }

    public class SeatDto
    {
        public string SeatName { get; set; }
        public string SeatType { get; set; }
        public int Row { get; set; }
        public int Col { get; set; }
    }
}
