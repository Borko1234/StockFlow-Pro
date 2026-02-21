using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlowPro.Data;
using StockFlowPro.ViewModels.Admin;

namespace StockFlowPro.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminPayrollController : Controller
    {
        private readonly FoodieDbContext _context;

        public AdminPayrollController(FoodieDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, decimal? rate)
        {
            var start = startDate ?? DateTime.Today.AddDays(-30);
            var end = endDate ?? DateTime.Today;
            var currentRate = rate ?? 0.50m;

            var query = _context.OrderProcessings
                .Include(op => op.PreparedByEmployee)
                .Where(op => op.PreparedByEmployeeId.HasValue && op.ProcessDate >= start && op.ProcessDate <= end.AddDays(1));

            var grouped = await query
                .GroupBy(op => op.PreparedByEmployeeId)
                .Select(g => new 
                {
                    EmployeeId = g.Key.Value,
                    Count = g.Count()
                })
                .ToListAsync();

            var employeeIds = grouped.Select(x => x.EmployeeId).ToList();
            var employees = await _context.Employees.Where(e => employeeIds.Contains(e.Id)).ToListAsync();

            var items = grouped.Select(g => 
            {
                var emp = employees.FirstOrDefault(e => e.Id == g.EmployeeId);
                return new EmployeePayrollItem
                {
                    EmployeeId = g.EmployeeId,
                    EmployeeName = emp?.FullName ?? "Unknown",
                    Position = emp?.Position ?? "Unknown",
                    OrdersCount = g.Count,
                    TotalAmount = g.Count * currentRate
                };
            }).ToList();

            var model = new PayrollViewModel
            {
                StartDate = start,
                EndDate = end,
                RatePerOrder = currentRate,
                Items = items
            };

            return View(model);
        }
    }
}
