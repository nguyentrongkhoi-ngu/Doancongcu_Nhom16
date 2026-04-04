using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.Data;
using CinemaBooking.Models;

namespace CinemaBooking.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class MoviesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MoviesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Movies
        public async Task<IActionResult> Index()
        {
            var movies = await _context.Phims
                .Include(p => p.NgonNguPhims)
                .Include(p => p.LichChieus)
                .OrderBy(p => p.TenPhim)
                .ToListAsync();

            return View(movies);
        }

        // GET: Admin/Movies/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var phim = await _context.Phims
                .Include(p => p.NgonNguPhims)
                .Include(p => p.LichChieus)
                    .ThenInclude(l => l.PhongChieu)
                        .ThenInclude(pc => pc.RapPhim)
                .Include(p => p.DanhGias)
                    .ThenInclude(d => d.NguoiDung)
                .FirstOrDefaultAsync(m => m.MaPhim == id);

            if (phim == null)
            {
                return NotFound();
            }

            return View(phim);
        }

        // GET: Admin/Movies/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Movies/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Phim phim, IFormFile? posterFile, IFormFile? trailerFile)
        {
            if (ModelState.IsValid)
            {
                // Handle file uploads
                if (posterFile != null && posterFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "posters");
                    Directory.CreateDirectory(uploadsFolder);
                    
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(posterFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);
                    
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await posterFile.CopyToAsync(stream);
                    }
                    
                    phim.UrlPoster = "/posters/" + fileName;
                }

                if (trailerFile != null && trailerFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "trailers");
                    Directory.CreateDirectory(uploadsFolder);
                    
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(trailerFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);
                    
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await trailerFile.CopyToAsync(stream);
                    }
                    
                    phim.Trailer = "/trailers/" + fileName;
                }

                _context.Add(phim);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Thêm phim thành công!";
                return RedirectToAction(nameof(Index));
            }
            
            return View(phim);
        }

        // GET: Admin/Movies/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var phim = await _context.Phims.FindAsync(id);
            if (phim == null)
            {
                return NotFound();
            }
            
            return View(phim);
        }

        // POST: Admin/Movies/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Phim phim, IFormFile? posterFile, IFormFile? trailerFile)
        {
            if (id != phim.MaPhim)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Handle file uploads
                    if (posterFile != null && posterFile.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "posters");
                        Directory.CreateDirectory(uploadsFolder);
                        
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(posterFile.FileName);
                        var filePath = Path.Combine(uploadsFolder, fileName);
                        
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await posterFile.CopyToAsync(stream);
                        }
                        
                        phim.UrlPoster = "/posters/" + fileName;
                    }

                    if (trailerFile != null && trailerFile.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "trailers");
                        Directory.CreateDirectory(uploadsFolder);
                        
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(trailerFile.FileName);
                        var filePath = Path.Combine(uploadsFolder, fileName);
                        
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await trailerFile.CopyToAsync(stream);
                        }
                        
                        phim.Trailer = "/trailers/" + fileName;
                    }

                    _context.Update(phim);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "Cập nhật phim thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PhimExists(phim.MaPhim))
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
            
            return View(phim);
        }

        // POST: Admin/Movies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var phim = await _context.Phims.FindAsync(id);
            if (phim != null)
            {
                _context.Phims.Remove(phim);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa phim thành công!";
            }
            
            return RedirectToAction(nameof(Index));
        }

        private bool PhimExists(int id)
        {
            return _context.Phims.Any(e => e.MaPhim == id);
        }
    }
}
