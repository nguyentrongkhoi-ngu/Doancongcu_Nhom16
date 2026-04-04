using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.Models;
using CinemaBooking.Extensions;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using CinemaBooking.Data;

namespace CinemaBooking.Controllers
{
    [Authorize]
    public class DatVeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DatVeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Hiển thị danh sách lịch chiếu của phim
        public async Task<IActionResult> Index(int maPhim)
        {
            var phim = await _context.Phims
                .Include(p => p.LichChieus)
                    .ThenInclude(l => l.PhongChieu)
                        .ThenInclude(p => p.RapPhim)
                .FirstOrDefaultAsync(p => p.MaPhim == maPhim);

            if (phim == null)
            {
                return NotFound();
            }

            // Lấy thời gian hiện tại
            var now = DateTime.Now;

            // Lấy các lịch chiếu từ thời điểm hiện tại trở đi (loại bỏ các lịch chiếu đã kết thúc)
            var lichChieus = phim.LichChieus
                .Where(l => l.NgayChieu.Date > now.Date ||
                      (l.NgayChieu.Date == now.Date &&
                       l.GioChieu > now.TimeOfDay))
                .OrderBy(l => l.NgayChieu)
                .ThenBy(l => l.GioChieu)
                .ToList();

            // Kiểm tra nếu không có lịch chiếu nào
            if (lichChieus.Count == 0)
            {
                TempData["InfoMessage"] = "Hiện tại chưa có lịch chiếu nào cho phim này.";
            }

            ViewBag.Phim = phim;
            return View(lichChieus);
        }

        // Hiển thị form đặt vé
        public async Task<IActionResult> Create(int maLichChieu)
        {
            var lichChieu = await _context.LichChieus
                .Include(l => l.Phim)
                .Include(l => l.PhongChieu)
                    .ThenInclude(p => p.RapPhim)
                .Include(l => l.PhongChieu)
                    .ThenInclude(p => p.Ghes)
                .FirstOrDefaultAsync(l => l.MaLichChieu == maLichChieu);

            if (lichChieu == null)
            {
                return NotFound();
            }

            // Kiểm tra xem lịch chiếu đã kết thúc chưa
            var now = DateTime.Now;
            if (lichChieu.NgayChieu.Date < now.Date ||
                (lichChieu.NgayChieu.Date == now.Date && lichChieu.GioChieu < now.TimeOfDay))
            {
                TempData["ErrorMessage"] = "Lịch chiếu này đã kết thúc, không thể đặt vé";
                return RedirectToAction("Index", "Home");
            }

            // Lấy danh sách ghế đã đặt
            var gheDaDat = await _context.DatVeGhes
                .Include(d => d.DatVe)
                .Where(d => d.DatVe.MaLichChieu == maLichChieu && d.DatVe.TrangThai != "Đã hủy")
                .Select(d => d.Ghe.SoGhe)
                .ToListAsync() ?? new List<string>();

            ViewBag.GheDaDat = gheDaDat;
            return View(lichChieu);
        }

        // Hiển thị giao diện chọn ghế
        public async Task<IActionResult> ChonGhe(int maLichChieu)
        {
            var lichChieu = await _context.LichChieus
                .Include(l => l.Phim)
                .Include(l => l.PhongChieu)
                    .ThenInclude(p => p.RapPhim)
                .Include(l => l.PhongChieu)
                    .ThenInclude(p => p.Ghes)
                .FirstOrDefaultAsync(l => l.MaLichChieu == maLichChieu);

            if (lichChieu == null)
            {
                return NotFound();
            }

            // Kiểm tra xem lịch chiếu đã kết thúc chưa
            var now = DateTime.Now;
            if (lichChieu.NgayChieu.Date < now.Date ||
                (lichChieu.NgayChieu.Date == now.Date && lichChieu.GioChieu < now.TimeOfDay))
            {
                TempData["ErrorMessage"] = "Lịch chiếu này đã kết thúc, không thể đặt vé";
                return RedirectToAction("Index", "Home");
            }

            // Lấy danh sách ghế đã đặt
            var gheDaDat = await _context.DatVeGhes
                .Include(d => d.DatVe)
                .Where(d => d.DatVe.MaLichChieu == maLichChieu && d.DatVe.TrangThai != "Đã hủy")
                .Select(d => d.Ghe.SoGhe)
                .ToListAsync() ?? new List<string>();

            // Tính toán số hàng và số cột dựa vào sức chứa của phòng
            var sucChua = lichChieu.PhongChieu.SucChua;
            int soCot = 0;
            char[] hangGhe = Array.Empty<char>();

            // Tính toán số cột dựa trên sức chứa
            if (sucChua <= 100)
            {
                soCot = 10; // Phòng nhỏ: 10 cột
            }
            else if (sucChua <= 150)
            {
                soCot = 15; // Phòng trung bình: 15 cột
            }
            else
            {
                soCot = 20; // Phòng lớn: 20 cột
            }

            // Tính số hàng cần thiết để đạt được sức chứa
            int soHang = (int)Math.Ceiling((double)sucChua / soCot);

            // Tạo mảng các ký tự hàng từ A đến Z
            hangGhe = Enumerable.Range(0, soHang)
                .Select(i => (char)('A' + i))
                .ToArray();

            ViewBag.GheDaDat = gheDaDat;
            ViewBag.HangGhe = hangGhe;
            ViewBag.SoCot = soCot;

            return View(lichChieu);
        }

        // API kiểm tra mã khuyến mãi
        [HttpPost]
        public async Task<IActionResult> KiemTraKhuyenMai(string maKhuyenMai)
        {
            if (string.IsNullOrEmpty(maKhuyenMai))
            {
                return Json(new { isValid = false });
            }

            var khuyenMai = await _context.KhuyenMais
                .FirstOrDefaultAsync(k => k.MaCode == maKhuyenMai &&
                                     k.NgayBatDau <= DateTime.Now &&
                                     k.NgayKetThuc >= DateTime.Now);

            if (khuyenMai == null)
            {
                return Json(new { isValid = false });
            }

            return Json(new {
                isValid = true,
                phanTramGiam = khuyenMai.PhanTramGiam,
                maKhuyenMai = khuyenMai.MaKhuyenMai
            });
        }

        // Xử lý đặt vé từ giao diện chọn ghế
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LuuDatVe(int maLichChieu, string selectedSeats, string maKhuyenMai = null)
        {
            if (string.IsNullOrEmpty(selectedSeats))
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một ghế";
                return RedirectToAction(nameof(ChonGhe), new { maLichChieu });
            }

            var selectedSeatsList = selectedSeats.Split(',');
            if (selectedSeatsList.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một ghế";
                return RedirectToAction(nameof(ChonGhe), new { maLichChieu });
            }

            var lichChieu = await _context.LichChieus
                .Include(l => l.Phim)
                .Include(l => l.PhongChieu)
                    .ThenInclude(p => p.Ghes)
                .FirstOrDefaultAsync(l => l.MaLichChieu == maLichChieu);

            if (lichChieu == null)
            {
                return NotFound();
            }

            // Kiểm tra ghế đã được đặt
            var gheDaDat = await _context.DatVeGhes
                .Include(d => d.DatVe)
                .Where(d => d.DatVe.MaLichChieu == maLichChieu && d.DatVe.TrangThai != "Đã hủy")
                .Select(d => d.Ghe.SoGhe)
                .ToListAsync();

            var conflictedSeats = selectedSeatsList.Where(s => gheDaDat.Contains(s)).ToList();
            if (conflictedSeats.Any())
            {
                TempData["ErrorMessage"] = $"Ghế {string.Join(", ", conflictedSeats)} đã được đặt. Vui lòng chọn ghế khác";
                return RedirectToAction(nameof(ChonGhe), new { maLichChieu });
            }

            // Tính tổng tiền dựa trên loại ghế
            decimal tongTien = 0;
            foreach (var soGhe in selectedSeatsList)
            {
                // Xác định loại ghế và giá tương ứng
                var row = soGhe[0]; // Ký tự đầu tiên là hàng (A, B, C...)
                var col = int.Parse(soGhe.Substring(1)); // Số ghế

                decimal giaGhe = lichChieu.GiaVe;

                // VIP: Hàng J-P, cột 1-3
                if (row >= 'J' && col >= 1 && col <= 3)
                {
                    giaGhe = lichChieu.GiaVe * 1.2m; // Ghế VIP giá cao hơn 20%
                }
                // Sweetbox: Hàng P, cột 1-4
                else if (row == 'P' && col >= 1 && col <= 4)
                {
                    giaGhe = lichChieu.GiaVe * 1.5m; // Sweetbox giá cao hơn 50%
                }

                tongTien += giaGhe;
            }

            // Áp dụng khuyến mãi nếu có
            int? maKM = null;
            if (!string.IsNullOrEmpty(maKhuyenMai))
            {
                var khuyenMai = await _context.KhuyenMais
                    .FirstOrDefaultAsync(k => k.MaCode == maKhuyenMai &&
                                         k.NgayBatDau <= DateTime.Now &&
                                         k.NgayKetThuc >= DateTime.Now);

                if (khuyenMai != null)
                {
                    tongTien = tongTien * (1 - khuyenMai.PhanTramGiam / 100);
                    maKM = khuyenMai.MaKhuyenMai;
                }
            }

            // Sử dụng transaction để đảm bảo tính nhất quán dữ liệu
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Kiểm tra lại một lần nữa để đảm bảo ghế vẫn còn trống
                    var latestGheDaDat = await _context.DatVeGhes
                        .Include(d => d.DatVe)
                        .Where(d => d.DatVe.MaLichChieu == maLichChieu && d.DatVe.TrangThai != "Đã hủy")
                        .Select(d => d.Ghe.SoGhe)
                        .ToListAsync();

                    var newConflictedSeats = selectedSeatsList.Where(s => latestGheDaDat.Contains(s)).ToList();
                    if (newConflictedSeats.Any())
                    {
                        await transaction.RollbackAsync();
                        TempData["ErrorMessage"] = $"Ghế {string.Join(", ", newConflictedSeats)} vừa được người khác đặt. Vui lòng chọn ghế khác";
                        return RedirectToAction(nameof(ChonGhe), new { maLichChieu });
                    }

                    // Tạo đơn đặt vé
                    var currentUserId = await User.GetLegacyUserIdAsync(_context);
                    if (!currentUserId.HasValue)
                    {
                        await transaction.RollbackAsync();
                        return RedirectToAction("Login", "Account");
                    }

                    var datVe = new DatVe
                    {
                        MaNguoiDung = currentUserId.Value,
                        MaLichChieu = maLichChieu,
                        NgayDat = DateTime.Now,
                        TongTien = tongTien,
                        TrangThai = "Chưa thanh toán",
                        MaKhuyenMai = maKM
                    };

                    _context.DatVes.Add(datVe);
                    await _context.SaveChangesAsync();

                    // Thêm chi tiết ghế
                    foreach (var soGhe in selectedSeatsList)
                    {
                        var ghe = await _context.Ghes
                            .FirstOrDefaultAsync(g => g.SoGhe == soGhe && g.MaPhong == lichChieu.MaPhong);

                        if (ghe != null)
                        {
                            var datVeGhe = new DatVeGhe
                            {
                                MaDatVe = datVe.MaDatVe,
                                MaGhe = ghe.MaGhe
                            };

                            _context.DatVeGhes.Add(datVeGhe);
                        }
                        else
                        {
                            // Nếu ghế không tồn tại trong cơ sở dữ liệu, tạo mới
                            var row = soGhe[0]; // Ký tự đầu tiên là hàng (A, B, C...)
                            var col = int.Parse(soGhe.Substring(1)); // Số ghế

                            // Xác định loại ghế
                            string loaiGhe = "Thường";

                            // Tính toán số cột dựa trên sức chứa
                            int soHangGhe = 10; // Mặc định là 10 hàng
                            var sucChua = lichChieu.PhongChieu.SucChua;
                            int soGheMotHang;

                            if (sucChua <= 100)
                            {
                                soGheMotHang = 10; // Phòng nhỏ: 10 cột
                            }
                            else if (sucChua <= 150)
                            {
                                soGheMotHang = 15; // Phòng trung bình: 15 cột
                            }
                            else
                            {
                                soGheMotHang = 20; // Phòng lớn: 20 cột
                            }

                            // Lấy danh sách hàng ghế
                            var hangGhe = Enumerable.Range(0, soHangGhe)
                                .Select(i => (char)('A' + i))
                                .ToArray();

                            // Tính vị trí hàng
                            int hangIndex = Array.IndexOf(hangGhe, row);
                            int tongSoHang = hangGhe.Length;

                            // VIP: 30% hàng cuối
                            if (hangIndex >= tongSoHang * 0.7)
                            {
                                loaiGhe = "VIP";
                            }

                            // Sweetbox: Hàng cuối, 4 ghế đầu tiên
                            if (row == hangGhe[hangGhe.Length - 1] && col <= 4)
                            {
                                loaiGhe = "Sweetbox";
                            }

                            var gheNew = new Ghe
                            {
                                MaPhong = lichChieu.MaPhong,
                                SoGhe = soGhe,
                                LoaiGhe = loaiGhe
                            };

                            _context.Ghes.Add(gheNew);
                            await _context.SaveChangesAsync();

                            var datVeGhe = new DatVeGhe
                            {
                                MaDatVe = datVe.MaDatVe,
                                MaGhe = gheNew.MaGhe
                            };

                            _context.DatVeGhes.Add(datVeGhe);
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Chuyển hướng đến trang thanh toán
                    return RedirectToAction("Index", "ThanhToan", new { maDatVe = datVe.MaDatVe });
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Có lỗi xảy ra khi đặt vé. Vui lòng thử lại sau.";
                    return RedirectToAction(nameof(ChonGhe), new { maLichChieu });
                }
            }
        }

        // Xử lý đặt vé
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int maLichChieu, string[] selectedSeats, string maKhuyenMai = null)
        {
            if (selectedSeats == null || selectedSeats.Length == 0)
            {
                ModelState.AddModelError("", "Vui lòng chọn ít nhất một ghế");
                return RedirectToAction(nameof(Create), new { maLichChieu });
            }

            var lichChieu = await _context.LichChieus
                .Include(l => l.Phim)
                .Include(l => l.PhongChieu)
                .FirstOrDefaultAsync(l => l.MaLichChieu == maLichChieu);

            if (lichChieu == null)
            {
                return NotFound();
            }

            // Kiểm tra ghế đã được đặt
            var gheDaDat = await _context.DatVeGhes
                .Include(d => d.DatVe)
                .Where(d => d.DatVe.MaLichChieu == maLichChieu && d.DatVe.TrangThai != "Đã hủy")
                .Select(d => d.Ghe.SoGhe)
                .ToListAsync();

            if (selectedSeats.Any(s => gheDaDat.Contains(s)))
            {
                ModelState.AddModelError("", "Một số ghế đã được đặt. Vui lòng chọn ghế khác");
                return RedirectToAction(nameof(Create), new { maLichChieu });
            }

            // Tính tổng tiền
            decimal tongTien = selectedSeats.Length * lichChieu.GiaVe;

            // Áp dụng khuyến mãi nếu có
            if (!string.IsNullOrEmpty(maKhuyenMai))
            {
                var khuyenMai = await _context.KhuyenMais
                    .FirstOrDefaultAsync(k => k.MaCode == maKhuyenMai && k.NgayKetThuc > DateTime.Now);

                if (khuyenMai != null)
                {
                    tongTien = tongTien * (1 - khuyenMai.PhanTramGiam / 100);
                }
            }

            // Tạo đơn đặt vé
            var currentUserId = await User.GetLegacyUserIdAsync(_context);
            if (!currentUserId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var datVe = new DatVe
            {
                MaNguoiDung = currentUserId.Value,
                MaLichChieu = maLichChieu,
                NgayDat = DateTime.Now,
                TongTien = tongTien,
                TrangThai = "Đang xử lý",
                MaKhuyenMai = !string.IsNullOrEmpty(maKhuyenMai) ?
                    (await _context.KhuyenMais.FirstOrDefaultAsync(k => k.MaCode == maKhuyenMai))?.MaKhuyenMai : (int?)null
            };

            _context.DatVes.Add(datVe);
            await _context.SaveChangesAsync();

            // Thêm chi tiết ghế
            foreach (var soGhe in selectedSeats)
            {
                var ghe = await _context.Ghes
                    .FirstOrDefaultAsync(g => g.SoGhe == soGhe && g.MaPhong == lichChieu.MaPhong);

                if (ghe != null)
                {
                    var datVeGhe = new DatVeGhe
                    {
                        MaDatVe = datVe.MaDatVe,
                        MaGhe = ghe.MaGhe
                    };

                    _context.DatVeGhes.Add(datVeGhe);
                }
                else
                {
                    // Nếu ghế không tồn tại trong cơ sở dữ liệu, tạo mới
                    var row = soGhe[0]; // Ký tự đầu tiên là hàng (A, B, C...)
                    var col = int.Parse(soGhe.Substring(1)); // Số ghế

                    // Xác định loại ghế
                    string loaiGhe = "Thường";

                    // Tính toán số cột dựa trên sức chứa
                    int soHangGhe = 10; // Mặc định là 10 hàng
                    var sucChua = lichChieu.PhongChieu.SucChua;
                    int soGheMotHang;

                    if (sucChua <= 100)
                    {
                        soGheMotHang = 10; // Phòng nhỏ: 10 cột
                    }
                    else if (sucChua <= 150)
                    {
                        soGheMotHang = 15; // Phòng trung bình: 15 cột
                    }
                    else
                    {
                        soGheMotHang = 20; // Phòng lớn: 20 cột
                    }

                    // Lấy danh sách hàng ghế
                    var hangGhe = Enumerable.Range(0, soHangGhe)
                        .Select(i => (char)('A' + i))
                        .ToArray();

                    // Tính vị trí hàng
                    int hangIndex = Array.IndexOf(hangGhe, row);
                    int tongSoHang = hangGhe.Length;

                    // VIP: 30% hàng cuối
                    if (hangIndex >= tongSoHang * 0.7)
                    {
                        loaiGhe = "VIP";
                    }

                    // Sweetbox: Hàng cuối, 4 ghế đầu tiên
                    if (row == hangGhe[hangGhe.Length - 1] && col <= 4)
                    {
                        loaiGhe = "Sweetbox";
                    }

                    var gheNew = new Ghe
                    {
                        MaPhong = lichChieu.MaPhong,
                        SoGhe = soGhe,
                        LoaiGhe = loaiGhe
                    };

                    _context.Ghes.Add(gheNew);
                    await _context.SaveChangesAsync();

                    var datVeGhe = new DatVeGhe
                    {
                        MaDatVe = datVe.MaDatVe,
                        MaGhe = gheNew.MaGhe
                    };

                    _context.DatVeGhes.Add(datVeGhe);
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "ThanhToan", new { maDatVe = datVe.MaDatVe });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HuyVe(int maDatVe)
        {
            var datVe = await _context.DatVes
                .Include(d => d.LichChieu)
                .FirstOrDefaultAsync(d => d.MaDatVe == maDatVe);

            if (datVe == null)
            {
                return NotFound();
            }

            // Kiểm tra xem vé có phải của người dùng hiện tại không
            var currentUserId = await User.GetLegacyUserIdAsync(_context);
            if (!currentUserId.HasValue || datVe.MaNguoiDung != currentUserId.Value)
            {
                return Forbid();
            }

            // Kiểm tra thời gian hủy vé (ít nhất 4 giờ trước giờ chiếu)
            DateTime lichChieuDateTime = datVe.LichChieu.NgayChieu.Date.Add(datVe.LichChieu.GioChieu);
            DateTime limitTime = lichChieuDateTime.AddHours(-4);

            if (DateTime.Now > limitTime)
            {
                TempData["ErrorMessage"] = "Không thể hủy vé trong vòng 4 giờ trước giờ chiếu.";
                return RedirectToAction("LichSuDatVe", "Home");
            }

            // Xác định mức hoàn tiền dựa trên trạng thái và thời gian
            decimal refundAmount = 0;
            bool canRefund = false;

            if (datVe.TrangThai == "Chưa thanh toán" || datVe.TrangThai == "Chờ thanh toán")
            {
                // Vé chưa thanh toán, có thể hủy mà không cần hoàn tiền
                canRefund = true;
            }
            else if (datVe.TrangThai == "Đã thanh toán")
            {
                // Tính khoảng cách thời gian đến lịch chiếu
                TimeSpan timeToScreening = lichChieuDateTime - DateTime.Now;

                // Không hoàn tiền theo yêu cầu mới
                    canRefund = true;
                refundAmount = 0;
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể hủy vé với trạng thái hiện tại.";
                return RedirectToAction("LichSuDatVe", "Home");
            }

            if (!canRefund)
            {
                TempData["ErrorMessage"] = "Không thể hủy vé này.";
                return RedirectToAction("LichSuDatVe", "Home");
            }

            // Sử dụng transaction để đảm bảo tính nhất quán dữ liệu
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Cập nhật trạng thái thanh toán thành "Đã hủy" thay vì xóa
                    var thanhToans = await _context.ThanhToans
                        .Where(t => t.MaDatVe == maDatVe)
                        .ToListAsync();

                    if (thanhToans.Any())
                    {
                        foreach (var thanhToan in thanhToans)
                        {
                            thanhToan.TrangThai = "Đã hủy";
                            thanhToan.GhiChu = $"Hủy vé ngày {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
                            _context.Update(thanhToan);
                        }
                        await _context.SaveChangesAsync();
                    }

                    // Truy vấn chi tiết đặt vé và ghế một cách an toàn
                    var datVeGhes = await _context.DatVeGhes
                        .Include(c => c.Ghe)
                        .Where(c => c.MaDatVe == maDatVe)
                        .ToListAsync();

                    // Lưu thông tin ghế trước khi xóa
                    string danhSachGhe = "Không có thông tin ghế";
                    if (datVeGhes != null && datVeGhes.Any())
                    {
                        var gheInfo = datVeGhes
                            .Where(dg => dg.Ghe != null)
                            .Select(dg => dg.Ghe.SoGhe)
                            .ToList();

                        if (gheInfo != null && gheInfo.Any())
                        {
                            danhSachGhe = string.Join(", ", gheInfo);
                        }

                        // Xóa các chi tiết đặt vé
                        _context.DatVeGhes.RemoveRange(datVeGhes);
                        await _context.SaveChangesAsync();
                    }

                    // Cập nhật trạng thái thành "Đã hủy" cho mọi trường hợp
                        datVe.TrangThai = "Đã hủy";
                        datVe.GhiChu = $"Ghế đã đặt: {danhSachGhe}";
                        _context.Update(datVe);

                    TempData["SuccessMessage"] = "Hủy vé thành công. Vé được hủy sẽ không được hoàn tiền.";

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Ghi nhật ký giao dịch
                    var lichSuGiaoDich = new LichSuGiaoDich
                    {
                        MaNguoiDung = currentUserId,
                        LoaiGiaoDich = "Hủy vé",
                        NoiDung = $"Hủy vé #{maDatVe}, danh sách ghế: {danhSachGhe}",
                        NgayGiaoDich = DateTime.Now,
                        TrangThai = "Thành công"
                    };
                    _context.LichSuGiaoDiches.Add(lichSuGiaoDich);
                    await _context.SaveChangesAsync();

                    return RedirectToAction("LichSuDatVe", "Home");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    // Ghi log lỗi
                    var lichSuLoi = new LichSuGiaoDich
                    {
                        MaNguoiDung = currentUserId,
                        LoaiGiaoDich = "Lỗi hủy vé",
                        NoiDung = $"Lỗi khi hủy vé #{maDatVe}. Chi tiết: {ex.Message}",
                        NgayGiaoDich = DateTime.Now,
                        TrangThai = "Thất bại"
                    };
                    _context.LichSuGiaoDiches.Add(lichSuLoi);
                    await _context.SaveChangesAsync();

                    TempData["ErrorMessage"] = "Có lỗi xảy ra khi hủy vé. Vui lòng thử lại sau hoặc liên hệ hỗ trợ.";
                    return RedirectToAction("LichSuDatVe", "Home");
                }
            }
        }

        // API lấy danh sách ghế đã đặt theo thời gian thực
        [HttpGet]
        public async Task<IActionResult> GetBookedSeats(int maLichChieu)
        {
            try
            {
                var lichChieu = await _context.LichChieus
                    .Include(l => l.PhongChieu)
                    .FirstOrDefaultAsync(l => l.MaLichChieu == maLichChieu);

                if (lichChieu == null)
                {
                    return Json(new {
                        success = false,
                        message = "Không tìm thấy lịch chiếu"
                    });
                }

                // Lấy danh sách ghế đã đặt
                var gheDaDat = await _context.DatVeGhes
                    .Include(d => d.DatVe)
                    .Where(d => d.DatVe.MaLichChieu == maLichChieu && d.DatVe.TrangThai != "Đã hủy")
                    .Select(d => d.Ghe.SoGhe)
                    .ToListAsync() ?? new List<string>();

                // Lấy thông tin hàng ghế và số cột
                int soCot = 0;
                char[] hangGhe = Array.Empty<char>();

                // Tính toán số cột dựa trên sức chứa
                var sucChua = lichChieu.PhongChieu.SucChua;
                if (sucChua <= 100)
                {
                    soCot = 10; // Phòng nhỏ: 10 cột
                }
                else if (sucChua <= 150)
                {
                    soCot = 15; // Phòng trung bình: 15 cột
                }
                else
                {
                    soCot = 20; // Phòng lớn: 20 cột
                }

                // Tính số hàng cần thiết để đạt được sức chứa
                int soHang = (int)Math.Ceiling((double)sucChua / soCot);

                // Tạo mảng các ký tự hàng từ A đến Z
                hangGhe = Enumerable.Range(0, soHang)
                    .Select(i => (char)('A' + i))
                    .ToArray();

                return Json(new {
                    success = true,
                    bookedSeats = gheDaDat,
                    hangGhe = hangGhe,
                    soCot = soCot
                });
            }
            catch (Exception ex)
            {
                return Json(new {
                    success = false,
                    message = "Lỗi khi lấy danh sách ghế: " + ex.Message
                });
            }
        }

        // In vé
        public async Task<IActionResult> Print(int maDatVe)
        {
            var datVe = await _context.DatVes
                .Include(d => d.LichChieu)
                    .ThenInclude(l => l.Phim)
                .Include(d => d.LichChieu)
                    .ThenInclude(l => l.PhongChieu)
                        .ThenInclude(p => p.RapPhim)
                .Include(d => d.DatVeGhes)
                    .ThenInclude(dg => dg.Ghe)
                .Include(d => d.NguoiDung)
                .Include(d => d.ThanhToans)
                .FirstOrDefaultAsync(d => d.MaDatVe == maDatVe);

            if (datVe == null)
            {
                return NotFound();
            }

            // Kiểm tra quyền truy cập: người dùng chỉ được in vé của mình
            var userId = await User.GetLegacyUserIdAsync(_context);
            if (!userId.HasValue || datVe.MaNguoiDung != userId.Value)
            {
                return Forbid();
            }

            // Kiểm tra vé phải đã thanh toán
            if (datVe.TrangThai != "Đã thanh toán")
            {
                TempData["ErrorMessage"] = "Vé chưa được thanh toán, không thể in.";
                return RedirectToAction("LichSuDatVe", "Home");
            }

            return View(datVe);
        }

        // In từng vé cho từng ghế
        public async Task<IActionResult> PrintMultiple(int maDatVe)
        {
            var datVe = await _context.DatVes
                .Include(d => d.LichChieu)
                    .ThenInclude(l => l.Phim)
                .Include(d => d.LichChieu)
                    .ThenInclude(l => l.PhongChieu)
                        .ThenInclude(p => p.RapPhim)
                .Include(d => d.DatVeGhes)
                    .ThenInclude(dg => dg.Ghe)
                .Include(d => d.NguoiDung)
                .Include(d => d.ThanhToans)
                .FirstOrDefaultAsync(d => d.MaDatVe == maDatVe);

            if (datVe == null)
            {
                return NotFound();
            }

            // Kiểm tra quyền truy cập: người dùng chỉ được in vé của mình
            var userId = await User.GetLegacyUserIdAsync(_context);
            if (!userId.HasValue || datVe.MaNguoiDung != userId.Value)
            {
                return Forbid();
            }

            // Kiểm tra vé phải đã thanh toán
            if (datVe.TrangThai != "Đã thanh toán")
            {
                TempData["ErrorMessage"] = "Vé chưa được thanh toán, không thể in.";
                return RedirectToAction("LichSuDatVe", "Home");
            }

            return View(datVe);
        }
    }
}