using System.ComponentModel.DataAnnotations;
using ErpMetronic.Domain.Common;

namespace ErpMetronic.Domain.Entities;

/// <summary>Saldo persediaan per batch/lot (sub-ledger di bawah ProductStock) + tanggal kedaluwarsa.</summary>
public class StockLot : BaseEntity
{
    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    [Required, StringLength(60)]
    public string LotNumber { get; set; } = string.Empty;

    public DateTime? ExpiryDate { get; set; }

    /// <summary>Jumlah tersisa pada lot ini.</summary>
    public int Quantity { get; set; }
}

/// <summary>Nomor seri unit (untuk produk yang dilacak per serial).</summary>
public class SerialNumber : BaseEntity
{
    public int ProductId { get; set; }
    public Product? Product { get; set; }

    [Required, StringLength(80)]
    public string SerialNo { get; set; } = string.Empty;

    /// <summary>Gudang lokasi saat ini (null bila sudah keluar).</summary>
    public int? WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    public bool IsInStock { get; set; } = true;
}
