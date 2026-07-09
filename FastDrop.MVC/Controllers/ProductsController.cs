using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FastDrop.MVC.Models;
using FastDrop.MVC.Services;

namespace FastDrop.MVC.Controllers
{
    public class ProductsController : Controller
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        // GET: Products
        public async Task<IActionResult> Index()
        {
            var products = await _productService.GetProductsAsync();
            return View(products);
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _productService.GetProductByIdAsync(id.Value);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: Products/Create
        public async Task<IActionResult> Create()
        {
            ViewData["MerchantId"] = new SelectList(await _productService.GetMerchantsAsync(), "MerchantId", "MerchantId");
            ViewBag.Warehouses = await _productService.GetWarehousesAsync();
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductId,Name,Description,Price,MerchantId")] Product product, [FromForm] int[] WarehouseIds, [FromForm] int[] Quantities)
        {
            if (ModelState.IsValid)
            {
                var warehouseQuantities = new Dictionary<int, int>();
                for (int i = 0; i < WarehouseIds.Length; i++)
                {
                    if (Quantities.Length > i && Quantities[i] > 0)
                    {
                        warehouseQuantities[WarehouseIds[i]] = Quantities[i];
                    }
                }

                await _productService.CreateProductAsync(product, warehouseQuantities);
                return RedirectToAction(nameof(Index));
            }
            ViewData["MerchantId"] = new SelectList(await _productService.GetMerchantsAsync(), "MerchantId", "MerchantId", product.MerchantId);
            ViewBag.Warehouses = await _productService.GetWarehousesAsync();
            return View(product);
        }

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _productService.GetProductByIdAsync(id.Value);
            if (product == null) return NotFound();

            // Load existing product warehouses for this product
            // Wait, GetProductByIdAsync doesn't include ProductWarehouses. Let's assume the view will just show all and rely on the controller. 
            // Better to fetch them directly or via DbContext if needed, but since we're in the controller:
            ViewBag.Warehouses = await _productService.GetWarehousesAsync();
            
            ViewData["MerchantId"] = new SelectList(await _productService.GetMerchantsAsync(), "MerchantId", "MerchantId", product.MerchantId);
            return View(product);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductId,Name,Description,Price,MerchantId")] Product product, [FromForm] int[] WarehouseIds, [FromForm] int[] Quantities)
        {
            if (id != product.ProductId) return NotFound();

            if (ModelState.IsValid)
            {
                var warehouseQuantities = new Dictionary<int, int>();
                for (int i = 0; i < WarehouseIds.Length; i++)
                {
                    if (Quantities.Length > i && Quantities[i] > 0)
                    {
                        warehouseQuantities[WarehouseIds[i]] = Quantities[i];
                    }
                }

                try
                {
                    await _productService.UpdateProductAsync(product, warehouseQuantities);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.ProductId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["MerchantId"] = new SelectList(await _productService.GetMerchantsAsync(), "MerchantId", "MerchantId", product.MerchantId);
            ViewBag.Warehouses = await _productService.GetWarehousesAsync();
            return View(product);
        }

        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _productService.GetProductForDeleteAsync(id.Value);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _productService.DeleteProductAsync(id);
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _productService.ProductExists(id);
        }
    }
}
