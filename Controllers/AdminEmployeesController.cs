using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlowPro.Data;
using StockFlowPro.Models;

namespace StockFlowPro.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminEmployeesController : Controller
    {
        private readonly FoodieDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public AdminEmployeesController(FoodieDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var employees = await _context.Employees.Include(e => e.User).ToListAsync();
            return View(employees);
        }

        public IActionResult Create()
        {
            ViewBag.CreateLogin = true;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Employee employee, string email, string password, bool createLogin)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Email = email;
                ViewBag.CreateLogin = createLogin;
                return View(employee);
            }

            if (createLogin)
            {
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    ModelState.AddModelError(string.Empty, "Email and Password are required for login.");
                    ViewBag.Email = email;
                    ViewBag.CreateLogin = createLogin;
                    return View(employee);
                }

                // Create User
                var normalizedEmail = NormalizeEmail(email);
                var user = new IdentityUser { UserName = normalizedEmail, Email = normalizedEmail, EmailConfirmed = true };
                var result = await _userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    string role = employee.Position switch
                    {
                        "Office" => "OfficeWorker",
                        "OfficeWorker" => "OfficeWorker",
                        "Scanner" => "Scanner",
                        "Packer" => "Packer",
                        _ => "OfficeWorker"
                    };
                    
                    await _userManager.AddToRoleAsync(user, role);
                    employee.UserId = user.Id;
                }
                else
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);
                    ViewBag.Email = normalizedEmail;
                    ViewBag.CreateLogin = createLogin;
                    return View(employee);
                }
            }

            _context.Add(employee);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private static string NormalizeEmail(string email)
        {
            var trimmed = email?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmed)) return string.Empty;
            return trimmed.Contains("@") ? trimmed : $"{trimmed}@stockflow.pro";
        }

        // Edit/Delete actions omitted for brevity but should be here. 
        // Re-implementing basic Delete for cleanup support.
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var employee = await _context.Employees.FirstOrDefaultAsync(m => m.Id == id);
            if (employee == null) return NotFound();
            return View(employee);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                if (!string.IsNullOrEmpty(employee.UserId))
                {
                    var user = await _userManager.FindByIdAsync(employee.UserId);
                    if (user != null) await _userManager.DeleteAsync(user);
                }
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
