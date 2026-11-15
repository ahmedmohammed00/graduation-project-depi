using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using FastDrop.MVC.Models;
using FastDrop.MVC.Security;
using FastDrop.MVC.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FastDrop.MVC.Services;

public class MerchantOperationsService(DmsDbContext context) : IMerchantOperationsService
{
    private const decimal MoneyMax = 99999999.99m;

    public async Task<MerchantShipmentEditViewModel?> GetShipmentEditAsync(int id, ClaimsPrincipal user)
    {
        var merchantId = MerchantIdentity.GetId(user);
        if (merchantId == null) return null;
        var shipment = await context.Shipments.AsNoTracking().Include(s => s.Address)
            .SingleOrDefaultAsync(s => s.ShipmentId == id && s.MerchantId == merchantId);
        return shipment == null ? null : new MerchantShipmentEditViewModel
        {
            ShipmentId = id, WeightKg = shipment.WeightKg,
            Address = new MerchantAddressInput { Street = shipment.Address?.Street ?? "",
                District = shipment.Address?.District ?? "", ZoneId = shipment.Address?.ZoneId ?? 0 }
        };
    }

    public async Task<MerchantProductEditViewModel?> GetProductEditAsync(int id, ClaimsPrincipal user)
    {
        var merchantId = MerchantIdentity.GetId(user);
        if (merchantId == null) return null;
        return await context.Products.AsNoTracking().Where(p => p.ProductId == id && p.MerchantId == merchantId)
            .Select(p => new MerchantProductEditViewModel { ProductId = p.ProductId, Name = p.Name,
                Description = p.Description, Price = p.Price, StockQuantity = p.StockQuantity,
                OriginalStockQuantity = p.StockQuantity }).SingleOrDefaultAsync();
    }

    public async Task<MerchantWriteResult> CreateShipmentAsync(AddShipmentViewModel model, ClaimsPrincipal user)
    {
        var merchantId = MerchantIdentity.GetId(user);
        if (merchantId == null || !await context.Merchants.AnyAsync(m => m.MerchantId == merchantId)) return MerchantWriteResult.NotFound;
        if (!Valid(model) || model.Shipment == null || !Valid(model.Shipment) ||
            model.Shipment.Address == null || !Valid(model.Shipment.Address))
            return MerchantWriteResult.Invalid("Enter a valid delivery address, zone and weight.");
        if (model.SelectedProducts == null || model.SelectedProducts.Count > 500)
            return MerchantWriteResult.Invalid("Choose products from your catalog.");
        var selected = model.SelectedProducts.Where(p => p.IsSelected).ToList();
        if (selected.Count is < 1 or > 100 || selected.Any(p => p.ProductId <= 0 || p.Quantity <= 0) ||
            selected.Select(p => p.ProductId).Distinct().Count() != selected.Count)
            return MerchantWriteResult.Invalid("Select 1 to 100 different products with positive quantities.");
        var zone = await ValidZoneAsync(model.Shipment.Address.ZoneId);
        if (zone == null) return MerchantWriteResult.Invalid("Choose an active delivery zone with valid pricing.");
        var ids = selected.Select(p => p.ProductId).ToList();
        var products = await context.Products.Where(p => p.MerchantId == merchantId && ids.Contains(p.ProductId))
            .OrderBy(p => p.ProductId).ToListAsync();
        if (products.Count != selected.Count) return MerchantWriteResult.Invalid("One or more products are unavailable in your catalog.");
        var quantities = selected.ToDictionary(p => p.ProductId, p => p.Quantity);
        var total = zone.CodAmount;
        foreach (var product in products)
        {
            if (!ValidMoney(product.Price) || product.StockQuantity < quantities[product.ProductId])
                return MerchantWriteResult.Invalid("A selected product has insufficient stock or invalid pricing. Review your quantities.");
            total += product.Price * quantities[product.ProductId];
            if (total > MoneyMax) return MerchantWriteResult.Invalid("The shipment total exceeds the supported amount.");
        }
        var shipment = new Shipment
        {
            MerchantId = merchantId, Address = NewAddress(model.Shipment.Address), WeightKg = model.Shipment.WeightKg,
            CodAmount = total, CreationDate = DateTime.UtcNow
        };
        foreach (var product in products)
        {
            var quantity = quantities[product.ProductId];
            product.StockQuantity -= quantity;
            shipment.ShipmentProducts.Add(new ShipmentProduct { ProductId = product.ProductId,
                Quantity = quantity, UnitPrice = product.Price });
        }
        shipment.ShipmentStatuses.Add(new ShipmentStatus { Status = "Pending", UpdatedAt = DateTime.UtcNow });
        context.Shipments.Add(shipment);
        // One SaveChanges transaction includes the address, order, line prices, status and stock reservations.
        // Stock, price and owner concurrency checks reject stale reads instead of overselling.
        var result = await SaveAsync();
        return result.StatusCode == 200 ? result with { ShipmentId = shipment.ShipmentId } : result;
    }

    public async Task<MerchantWriteResult> EditShipmentAsync(int id, MerchantShipmentEditViewModel model, ClaimsPrincipal user)
    {
        var merchantId = MerchantIdentity.GetId(user);
        if (merchantId == null) return MerchantWriteResult.NotFound;
        var shipment = await context.Shipments.Include(s => s.Address).Include(s => s.ShipmentProducts)
            .SingleOrDefaultAsync(s => s.ShipmentId == id && s.MerchantId == merchantId);
        if (shipment == null) return MerchantWriteResult.NotFound;
        if (!Valid(model) || model.Address == null || !Valid(model.Address))
            return MerchantWriteResult.Invalid("Enter a valid delivery address, zone and weight.");
        var zone = await ValidZoneAsync(model.Address.ZoneId);
        if (zone == null) return MerchantWriteResult.Invalid("Choose an active delivery zone with valid pricing.");
        if (shipment.Address?.ZoneId != model.Address.ZoneId)
        {
            if (shipment.ShipmentProducts.Count == 0 || shipment.ShipmentProducts.Any(p => !p.UnitPrice.HasValue))
                return MerchantWriteResult.Invalid("This older shipment has no saved item prices. Ask a dispatcher to change its delivery zone.");
            var total = zone.CodAmount + shipment.ShipmentProducts.Sum(p => p.UnitPrice!.Value * p.Quantity);
            if (!ValidMoney(total)) return MerchantWriteResult.Invalid("The shipment total exceeds the supported amount.");
            shipment.CodAmount = total;
        }
        // Addresses may be shared with another tenant, warehouse or route. Never edit the shared row.
        shipment.Address = NewAddress(model.Address);
        shipment.WeightKg = model.WeightKg;
        return await SaveAsync();
    }

    public async Task<MerchantWriteResult> EditProductAsync(int id, MerchantProductEditViewModel model, ClaimsPrincipal user)
    {
        var merchantId = MerchantIdentity.GetId(user);
        if (merchantId == null) return MerchantWriteResult.NotFound;
        var product = await context.Products.SingleOrDefaultAsync(p => p.ProductId == id && p.MerchantId == merchantId);
        if (product == null) return MerchantWriteResult.NotFound;
        if (!Valid(model) || !ValidMoney(model.Price)) return MerchantWriteResult.Invalid("Enter valid product details, price and stock.");
        if (product.StockQuantity != model.OriginalStockQuantity)
            return new(409, "Stock changed while you were editing. Reload the product before saving.");
        product.Name = model.Name.Trim();
        product.Description = model.Description?.Trim();
        product.Price = model.Price;
        product.StockQuantity = model.StockQuantity;
        // Only these allowlisted scalar properties are changed; no detached graph is attached.
        return await SaveAsync();
    }

    private async Task<Zone?> ValidZoneAsync(int id) => await context.Zones.AsNoTracking()
        .SingleOrDefaultAsync(z => z.ZoneId == id && z.ActiveStatus && z.CodAmount >= 0 && z.CodAmount <= MoneyMax && decimal.Round(z.CodAmount, 2) == z.CodAmount);

    private static Address NewAddress(MerchantAddressInput address) => new()
    {
        Street = address.Street.Trim(), District = address.District.Trim(), ZoneId = address.ZoneId
    };
    private static bool Valid(object model) => Validator.TryValidateObject(model, new ValidationContext(model), null, true);
    private static bool ValidMoney(decimal value) => value >= 0 && value <= MoneyMax && decimal.Round(value, 2) == value;

    private async Task<MerchantWriteResult> SaveAsync()
    {
        try { await context.SaveChangesAsync(); return new(200); }
        catch (DbUpdateException)
        {
            // Relational SaveChanges rolls back the whole write. Discard the failed tracked reservation too.
            context.ChangeTracker.Clear();
            return new(409, "The order or inventory changed before it could be saved. Reload and try again.");
        }
    }
}
