using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.Data;
using CinemaBooking.Models;
using CinemaBooking.Extensions;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace CinemaBooking.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string SESSION_BOOKING_KEY = "BookingBasket";

        public BookingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Booking/SelectSeats/{lichChieuId}
        public async Task<IActionResult> SelectSeats(int id)
        {
            var lichChieu = await _context.LichChieus
                .Include(l => l.Phim)
                .Include(l => l.PhongChieu)
                    .ThenInclude(p => p.RapPhim)
                .Include(l => l.PhongChieu)
                    .ThenInclude(p => p.Ghes)
                .FirstOrDefaultAsync(l => l.MaLichChieu == id);

            if (lichChieu == null) return NotFound();

            // Lấy ghế đã được xác nhận (Đã thanh toán hoặc đang xử lý)
            var occupiedSeats = await _context.DatVeGhes
                .Include(d => d.DatVe)
                .Where(d => d.DatVe.MaLichChieu == id && d.DatVe.TrangThai != "Đã hủy")
                .Select(d => d.Ghe.SoGhe)
                .ToListAsync();

            ViewBag.OccupiedSeats = occupiedSeats;

            // Tính toán xếp hạng thành viên
            var currentUserId = await User.GetLegacyUserIdAsync(_context);
            if (currentUserId.HasValue)
            {
                var totalSpent = await _context.DatVes
                    .Where(d => d.MaNguoiDung == currentUserId.Value && d.TrangThai == "Đã thanh toán")
                    .SumAsync(d => d.TongTien);

                string rank = "Đồng";
                decimal discount = 0;

                if (totalSpent >= 10000000) { rank = "Kim Cương"; discount = 15; }
                else if (totalSpent >= 5000000) { rank = "Vàng"; discount = 10; }
                else if (totalSpent >= 1000000) { rank = "Bạc"; discount = 5; }

                ViewBag.UserRank = rank;
                ViewBag.DiscountPercent = discount;
            }

            return View(lichChieu);
        }

        // POST: /Booking/AddToCart
        [HttpPost]
        public IActionResult AddToCart([FromBody] SeatSelectionRequest request)
        {
            if (request == null) return BadRequest();

            var basket = GetBasket();
            if (basket.LichChieuId != request.LichChieuId)
            {
                basket = new BookingBasket { LichChieuId = request.LichChieuId };
            }

            if (!basket.SelectedSeats.Contains(request.SeatId))
            {
                if (basket.SelectedSeats.Count >= 8) 
                    return Json(new { success = false, message = "Tối đa 8 ghế" });
                
                basket.SelectedSeats.Add(request.SeatId);
                SaveBasket(basket);
            }

            return Json(new { success = true, count = basket.SelectedSeats.Count });
        }

        // POST: /Booking/RemoveFromCart
        [HttpPost]
        public IActionResult RemoveFromCart([FromBody] SeatSelectionRequest request)
        {
            if (request == null) return BadRequest();

            var basket = GetBasket();
            if (basket.LichChieuId == request.LichChieuId)
            {
                basket.SelectedSeats.Remove(request.SeatId);
                SaveBasket(basket);
            }

            return Json(new { success = true, count = basket.SelectedSeats.Count });
        }

        // POST: /Booking/Confirm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int maLichChieu, string selectedSeats)
        {
            if (string.IsNullOrEmpty(selectedSeats))
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một ghế";
                return RedirectToAction(nameof(SelectSeats), new { id = maLichChieu });
            }

            // Chuyển sang ChonCombo logic (tương tự DatVeController nhưng chuyển vùng sang Booking)
            // Hoặc có thể trực tiếp xử lý giỏ hàng ở đây.
            
            // Theo yêu cầu của dự án: Lưu thông tin vào session và chuyển sang bước tiếp theo.
            var seats = selectedSeats.Split(',').ToList();
            var basket = new BookingBasket { LichChieuId = maLichChieu, SelectedSeats = seats };
            SaveBasket(basket);

            return RedirectToAction("ChonCombo", new { maLichChieu });
        }

        public async Task<IActionResult> ChonCombo(int maLichChieu)
        {
            var basket = GetBasket();
            if (basket.LichChieuId != maLichChieu || !basket.SelectedSeats.Any())
            {
                return RedirectToAction(nameof(SelectSeats), new { id = maLichChieu });
            }

            var lichChieu = await _context.LichChieus
                .Include(l => l.Phim)
                .Include(l => l.PhongChieu)
                    .ThenInclude(p => p.RapPhim)
                .FirstOrDefaultAsync(l => l.MaLichChieu == maLichChieu);

            if (lichChieu == null) return NotFound();

            var combos = await _context.Combos
                .Where(c => c.TrangThai == true)
                .OrderByDescending(c => c.UuTien)
                .ToListAsync();

            ViewBag.SelectedSeats = string.Join(",", basket.SelectedSeats);
            ViewBag.Combos = combos;
            
            return View(lichChieu);
        }

        private BookingBasket GetBasket()
        {
            var sessionData = HttpContext.Session.GetString(SESSION_BOOKING_KEY);
            return sessionData == null ? new BookingBasket() : JsonSerializer.Deserialize<BookingBasket>(sessionData)!;
        }

        private void SaveBasket(BookingBasket basket)
        {
            HttpContext.Session.SetString(SESSION_BOOKING_KEY, JsonSerializer.Serialize(basket));
        }
    }

    public class BookingBasket
    {
        public int LichChieuId { get; set; }
        public List<string> SelectedSeats { get; set; } = new List<string>();
    }

    public class SeatSelectionRequest
    {
        public int LichChieuId { get; set; }
        public string SeatId { get; set; } = string.Empty;
    }
}
