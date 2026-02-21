using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockFlowPro.Data;
using StockFlowPro.Models;

namespace StockFlowPro.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminEmployeesController : Controller
    {
        private readonly StockFlowDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<AdminEmployeesController> _logger;

        public AdminEmployeesController(StockFlowDbContext context, UserManager<IdentityUser> userManager, ILogger<AdminEmployeesController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
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
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                _logger.LogWarning("Employee creation model validation failed: {Errors}", string.Join("; ", errors));
                return View(employee);
            }

            var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            var result = await _userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                var identityErrors = result.Errors.Select(e => $"{e.Code}: {e.Description}");
                _logger.LogWarning("Identity user creation failed for {Email}: {Errors}", email, string.Join("; ", identityErrors));
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(employee);
            }

            string role = employee.Position switch
            {
                "Office" => "OfficeWorker",
                "OfficeWorker" => "OfficeWorker",
                "Scanner" => "Scanner",
                "Packer" => "Packer",
                "Driver" => "Driver",
                _ => "OfficeWorker"
            };

            var roleResult = await _userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                var roleErrors = roleResult.Errors.Select(e => $"{e.Code}: {e.Description}");
                _logger.LogWarning("Role assignment failed for {Email} role {Role}: {Errors}", email, role, string.Join("; ", roleErrors));
                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(employee);
            }

            employee.UserId = user.Id;
            try
            {
                _context.Add(employee);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Employee save failed for {Email} {Name}", email, employee.FullName);
                ModelState.AddModelError(string.Empty, "Could not save employee to the database.");
                return View(employee);
            }
            return RedirectToAction(nameof(Index));
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
