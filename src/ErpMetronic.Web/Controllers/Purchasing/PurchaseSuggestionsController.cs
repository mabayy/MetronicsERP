using ErpMetronic.Domain.Constants;
using ErpMetronic.Domain.Entities;
using ErpMetronic.Domain.Enums;
using ErpMetronic.Infrastructure.Persistence;
using ErpMetronic.Infrastructure.Services;
using ErpMetronic.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Web.Controllers;

/// <summary>Saran Pembelian: produk yang stoknya ≤ titik reorder + buat PO draft dari saran.</summary>
[Authorize]
public class PurchaseSuggestionsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IDocumentNumberService _docNumber;

    public PurchaseSuggestionsController(ApplicationDbContext db, IDocumentNumberService docNumber)
    {
        _db = db;
        _docNumber = docNumber;
    }

    public async Task<IActionResult> Index()
    {
        var rows = await BuildSuggestionsAsync();
        await PopulateAsync();
        return View(rows);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePo(ReorderPoViewModel model)
    {
        var lines = model.Lines.Where(l => l.Selected && l.ProductId > 0 && l.Quantity > 0).ToList();
        if (lines.Count == 0) ModelState.AddModelError(string.Empty, "Pilih minimal satu produk dengan jumlah > 0.");
        if (!ModelState.IsValid)
        {
            var rows = await BuildSuggestionsAsync();
            await PopulateAsync();
            TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Input tidak valid.";
            return View(nameof(Index), rows);
        }

        var po = new PurchaseOrder
        {
            ReferenceNumber = await _docNumber.NextAsync(DocumentCodes.PurchaseOrder, DateTime.Today),
            OrderDate = DateTime.Today,
            SupplierId = model.SupplierId,
            WarehouseId = model.WarehouseId,
            Status = PurchaseOrderStatus.Draft,
            Note = "Dibuat dari Saran Pembelian (reorder)",
            CreatedBy = User.Identity?.Name,
            Items = lines.Select(l => new PurchaseOrderItem { ProductId = l.ProductId, Quantity = l.Quantity, UnitPrice = l.UnitPrice }).ToList()
        };
        _db.PurchaseOrders.Add(po);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Purchase Order {po.ReferenceNumber} dibuat (Draft) dari {lines.Count} saran.";
        return RedirectToAction("Details", "PurchaseOrders", new { id = po.Id });
    }

    private async Task<List<SuggestionRow>> BuildSuggestionsAsync()
    {
        var products = await _db.Products.Where(p => p.ReorderLevel > 0 && p.StockQuantity <= p.ReorderLevel)
            .OrderBy(p => p.Name).ToListAsync();
        return products.Select(p => new SuggestionRow
        {
            ProductId = p.Id,
            Sku = p.Sku,
            Name = p.Name,
            Stock = p.StockQuantity,
            ReorderLevel = p.ReorderLevel,
            SuggestedQty = p.ReorderQuantity > 0 ? p.ReorderQuantity : Math.Max(p.ReorderLevel - p.StockQuantity, 1),
            PurchasePrice = p.PurchasePrice
        }).ToList();
    }

    private async Task PopulateAsync()
    {
        ViewBag.Suppliers = new SelectList(await _db.Suppliers.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync(), "Id", "Name");
        ViewBag.Warehouses = new SelectList(await _db.Warehouses.OrderBy(w => w.Name).ToListAsync(), "Id", "Name");
    }
}
