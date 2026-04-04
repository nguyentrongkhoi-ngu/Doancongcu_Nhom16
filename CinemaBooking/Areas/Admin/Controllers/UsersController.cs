using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.Data;
using CinemaBooking.Models;

namespace CinemaBooking.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public UsersController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Admin/Users
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users
                .OrderBy(u => u.UserName)
                .ToListAsync();

            var userViewModels = new List<object>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userViewModels.Add(new
                {
                    User = user,
                    Roles = roles
                });
            }

            ViewBag.Users = userViewModels;
            return View();
        }

        // GET: Admin/Users/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.Roles = roles;

            return View(user);
        }

        // GET: Admin/Users/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var allRoles = await _roleManager.Roles.ToListAsync();

            ViewBag.UserRoles = userRoles;
            ViewBag.AllRoles = allRoles;

            return View(user);
        }

        // POST: Admin/Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, ApplicationUser model, string[] selectedRoles)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Update user properties
            user.HoTen = model.HoTen;
            user.SoDienThoai = model.SoDienThoai;
            user.Email = model.Email;
            user.UserName = model.UserName;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                // Update roles
                var userRoles = await _userManager.GetRolesAsync(user);
                var rolesToRemove = userRoles.Except(selectedRoles ?? new string[0]);
                var rolesToAdd = (selectedRoles ?? new string[0]).Except(userRoles);

                await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                await _userManager.AddToRolesAsync(user, rolesToAdd);

                TempData["SuccessMessage"] = "Cập nhật người dùng thành công!";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            var allRoles = await _roleManager.Roles.ToListAsync();
            ViewBag.UserRoles = selectedRoles ?? new string[0];
            ViewBag.AllRoles = allRoles;

            return View(model);
        }

        // POST: Admin/Users/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Xóa người dùng thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể xóa người dùng!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
