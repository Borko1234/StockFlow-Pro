using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlowPro.Data;
using StockFlowPro.Models;
using StockFlowPro.ViewModels.Admin;

namespace StockFlowPro.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminProductsController : Controller
    {
        private readonly FoodieDbContext _context;

        public AdminProductsController(FoodieDbContext context)
        {
            _context = context;
        }

        // GET: AdminProducts
        public async Task<IActionResult> Index(string? search = null, string? filter = "all", string? sortBy = "name", string? sortDir = "asc")
        {
            var query = _context.Products.AsQueryable();

            // Search
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.Contains(search) || p.Brand.Contains(search));
            }

            // Filter
            if (filter == "low")
            {
                query = query.Where(p => p.QuantityInStock <= p.MinimumStockLevel);
            }

            // Sort
            query = sortBy switch
            {
                "name" => sortDir == "asc" ? query.OrderBy(p => p.Name) : query.OrderByDescending(p => p.Name),
                "stock" => sortDir == "asc" ? query.OrderBy(p => p.QuantityInStock) : query.OrderByDescending(p => p.QuantityInStock),
                "price" => sortDir == "asc" ? query.OrderBy(p => p.Price) : query.OrderByDescending(p => p.Price),
                _ => query.OrderBy(p => p.Name)
            };

            var model = new ProductListViewModel
            {
                Products = await query.ToListAsync(),
                Search = search,
                Filter = filter,
                SortBy = sortBy,
                SortDir = sortDir,
                TotalCount = await _context.Products.CountAsync(),
                LowStockCount = await _context.Products.CountAsync(p => p.QuantityInStock <= p.MinimumStockLevel)
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> LookupByProductId(string productId)
        {
            if (!int.TryParse(productId, out var id))
                return Json(new { found = false });

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return Json(new { found = false });

            return Json(new { 
                found = true, 
                id = product.Id, 
                name = product.Name, 
                quantity = product.QuantityInStock
            });
        }

        // POST: AdminProducts/QuickUpdateQuantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickUpdateQuantity(int id, int delta)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            product.QuantityInStock += delta;
            if (product.QuantityInStock < 0) product.QuantityInStock = 0;
            
            await _context.SaveChangesAsync();

            return Json(new { success = true, newQuantity = product.QuantityInStock });
        }

        // GET: AdminProducts/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: AdminProducts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // GET: AdminProducts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        // POST: AdminProducts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            if (id != product.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // GET: AdminProducts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null) return NotFound();

            return View(product);
        }

        // POST: AdminProducts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}
