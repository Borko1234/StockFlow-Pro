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
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Employee employee, string email, string password)
        {
            if (ModelState.IsValid)
            {
                // Create User
                var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
                var result = await _userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    // Assign Role based on Position
                    if (!string.IsNullOrEmpty(employee.Position))
                    {
                        // Ensure role exists or use valid roles
                        string role = employee.Position switch
                        {
                            "Office" => "OfficeWorker", // Map if necessary, or ensure input matches Role Name
                            "Scanner" => "Scanner",
                            "Packer" => "Packer",
                            _ => "OfficeWorker" // Default
                        };
                        
                        // Check if role exists
                        // For simplicity assuming roles exist as seeded
                        await _userManager.AddToRoleAsync(user, role);
                    }

                    employee.UserId = user.Id;
                    _context.Add(employee);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(employee);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();
            return View(employee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FullName,Position,Phone,IsActive,UserId,CreatedAt")] Employee employee)
        {
            if (id != employee.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(employee);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Employees.Any(e => e.Id == employee.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(employee);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(m => m.Id == id);
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
                // Delete User (Cascade will delete Employee)
                var user = await _userManager.FindByIdAsync(employee.UserId);
                if (user != null)
                {
                    await _userManager.DeleteAsync(user);
                }
                else
                {
                    // If User missing, delete Employee manually
                    _context.Employees.Remove(employee);
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
