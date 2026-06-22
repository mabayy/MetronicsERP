using ErpMetronic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Web.Controllers;

/// <summary>Laporan telusur persediaan: stok per batch/lot (+ kedaluwarsa) & nomor seri.</summary>
[Authorize]
public class StockTrackingController : Controller
{
    private readonly ApplicationDbContext _db;
    public StockTrackingController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Lots()
    {
        var lots = await _db.StockLots.Include(l => l.Product).Include(l => l.Warehouse)
            .Where(l => l.Quantity > 0)
            .OrderBy(l => l.ExpiryDate == null).ThenBy(l => l.ExpiryDate).ThenBy(l => l.Product!.Name)
            .Take(500).ToListAsync();
        return View(lots);
    }

    public async Task<IActionResult> Serials(string? status)
    {
        var q = _db.SerialNumbers.Include(s => s.Product).Include(s => s.Warehouse).AsQueryable();
        if (status == "in") q = q.Where(s => s.IsInStock);
        else if (status == "out") q = q.Where(s => !s.IsInStock);
        ViewBag.Status = status;
        var list = await q.OrderByDescending(s => s.IsInStock).ThenBy(s => s.Product!.Name).ThenBy(s => s.SerialNo)
            .Take(500).ToListAsync();
        return View(list);
    }
}
