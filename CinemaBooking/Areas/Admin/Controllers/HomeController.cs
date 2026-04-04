using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.Data;
using CinemaBooking.Models;
using CinemaBooking.Models.ViewModels;

namespace CinemaBooking.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Dashboard statistics
            var stats = new DashboardViewModel
            {
                TotalUsers = await _context.NguoiDungs.CountAsync(),
                TotalMovies = await _context.Phims.CountAsync(),
                TotalBookings = await _context.DatVes.CountAsync(),
                TotalRevenue = await _context.ThanhToans
                    .Where(t => t.TrangThai == "Thành công")
                    .SumAsync(t => t.SoTien),
                RecentBookings = await _context.DatVes
                    .Include(d => d.NguoiDung)
                    .Include(d => d.LichChieu)
                        .ThenInclude(l => l.Phim)
                    .OrderByDescending(d => d.NgayDat)
                    .Take(10)
                    .ToListAsync()
            };

            ViewBag.Stats = stats;
            return View();
        }

        public IActionResult Users()
        {
            return View();
        }

        public IActionResult Movies()
        {
            return View();
        }

        public IActionResult Bookings()
        {
            return View();
        }

        public IActionResult Reports()
        {
            return View();
        }
    }
}
