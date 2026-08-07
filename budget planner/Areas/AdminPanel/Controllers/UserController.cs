using budget_planner.DAL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using budget_planner.Areas.ViewModel;
using budget_planner.DAL;
using budget_planner.Models;

namespace budget_planner.Areas.AdminPanel.Controllers
{
    [Authorize(Roles = "Admin")]
    [Area("AdminPanel")]
    public class UserController : Controller
    {
        private readonly BudgetDbContext _context;

        public UserController(BudgetDbContext context)
        {
            _context = context;
        }

        // GET: AdminPanel/User
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users
                .Where(u => !u.IsDeleted)
                .ToListAsync();

            return View(users);
        }

        // GET: AdminPanel/User/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: AdminPanel/User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateVM createVM)
        {
            if (!ModelState.IsValid)
            {
                return View(createVM);
            }

            bool isEmailExist = await _context.Users.AnyAsync(u => u.Email == createVM.Email);
            if (isEmailExist)
            {
                ModelState.AddModelError("Email", "Bu E-poçt ünvanı artıq istifadə olunur.");
                return View(createVM);
            }

            bool isUserNameExist = await _context.Users.AnyAsync(u => u.UserName == createVM.UserName);
            if (isUserNameExist)
            {
                ModelState.AddModelError("UserName", "Bu istifadəçi adı artıq mövcuddur.");
                return View(createVM);
            }

            ApplicationUser newUser = new ApplicationUser
            {
                FullName = createVM.FullName,
                UserName = createVM.UserName,
                Email = createVM.Email,
                IsActive = true,
                IsDeleted = false,
                CreatedDate = DateTime.Now
            };

            await _context.Users.AddAsync(newUser);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: AdminPanel/User/Update/5
        public async Task<IActionResult> Update(string? id)
        {
            if (id == null) return BadRequest();

            var existUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

            if (existUser == null) return NotFound();

            CreateVM updateVM = new CreateVM
            {
                Id = existUser.Id,
                FullName = existUser.FullName,
                UserName = existUser.UserName,
                Email = existUser.Email,
                IsActive = existUser.IsActive
            };

            return View(updateVM);
        }

        // POST: AdminPanel/User/Update/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(string? id, CreateVM updateVM)
        {
            if (id == null || id != updateVM.Id) return BadRequest();

            if (!ModelState.IsValid) return View(updateVM);

            var existUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

            if (existUser == null) return NotFound();

            // Öz ID-si xaricində başqa istifadəçidə bu Email var?
            bool isEmailExist = await _context.Users.AnyAsync(u => u.Id != id && u.Email == updateVM.Email);
            if (isEmailExist)
            {
                ModelState.AddModelError("Email", "Bu E-poçt ünvanı başqa bir istifadəçidə var.");
                return View(updateVM);
            }

            // Öz ID-si xaricində başqa istifadəçidə bu UserName var?
            bool isUserNameExist = await _context.Users.AnyAsync(u => u.Id != id && u.UserName == updateVM.UserName);
            if (isUserNameExist)
            {
                ModelState.AddModelError("UserName", "Bu istifadəçi adı artıq götürülüb.");
                return View(updateVM);
            }

            existUser.FullName = updateVM.FullName;
            existUser.UserName = updateVM.UserName;
            existUser.Email = updateVM.Email;
            existUser.IsActive = updateVM.IsActive;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: AdminPanel/User/ToggleStatus/5 (Aktiv/Bloklama əməliyyatı)
        public async Task<IActionResult> ToggleStatus(string? id)
        {
            if (id == null) return BadRequest();

            var existUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

            if (existUser == null) return NotFound();

            existUser.IsActive = !existUser.IsActive;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: AdminPanel/User/Delete/5 (Soft Delete)
        public async Task<IActionResult> Delete(string? id)
        {
            if (id == null) return BadRequest();

            var existUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

            if (existUser == null) return NotFound();

            existUser.IsDeleted = true;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}