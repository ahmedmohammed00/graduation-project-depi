using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FastDrop.MVC.Models;
using FastDrop.MVC.Services;

namespace FastDrop.MVC.Controllers
{
    public class MerchantsController : Controller
    {
        private readonly IMerchantService _merchantService;

        public MerchantsController(IMerchantService merchantService)
        {
            _merchantService = merchantService;
        }

        // GET: Merchants
        public async Task<IActionResult> Index()
        {
            var merchants = await _merchantService.GetMerchantsAsync();
            return View(merchants);
        }

        // GET: Merchants/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var merchant = await _merchantService.GetMerchantByIdAsync(id.Value);
            if (merchant == null)
            {
                return NotFound();
            }

            return View(merchant);
        }

        // GET: Merchants/Create
        public async Task<IActionResult> Create()
        {
            ViewData["AddressId"] = new SelectList(await _merchantService.GetAddressesAsync(), "AddressId", "AddressId");
            return View();
        }

        // POST: Merchants/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MerchantId,ShopName,Email,Password,ContactName,Phone,Status,AddressId")] Merchant merchant)
        {
            if (ModelState.IsValid)
            {
                await _merchantService.CreateMerchantAsync(merchant);
                return RedirectToAction(nameof(Index));
            }
            ViewData["AddressId"] = new SelectList(await _merchantService.GetAddressesAsync(), "AddressId", "AddressId", merchant.AddressId);
            return View(merchant);
        }

        // GET: Merchants/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var merchant = await _merchantService.GetMerchantByIdAsync(id.Value);
            if (merchant == null)
            {
                return NotFound();
            }
            ViewData["AddressId"] = new SelectList(await _merchantService.GetAddressesAsync(), "AddressId", "AddressId", merchant.AddressId);
            return View(merchant);
        }

        // POST: Merchants/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MerchantId,ShopName,Email,Password,ContactName,Phone,Status,AddressId")] Merchant merchant)
        {
            if (id != merchant.MerchantId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _merchantService.UpdateMerchantAsync(merchant);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MerchantExists(merchant.MerchantId))
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
            ViewData["AddressId"] = new SelectList(await _merchantService.GetAddressesAsync(), "AddressId", "AddressId", merchant.AddressId);
            return View(merchant);
        }

        // GET: Merchants/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var merchant = await _merchantService.GetMerchantForDeleteAsync(id.Value);
            if (merchant == null)
            {
                return NotFound();
            }

            return View(merchant);
        }

        // POST: Merchants/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _merchantService.DeleteMerchantAsync(id);
            return RedirectToAction(nameof(Index));
        }

        private bool MerchantExists(int id)
        {
            return _merchantService.MerchantExists(id);
        }
    }
}
