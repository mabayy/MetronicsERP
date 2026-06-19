using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ErpMetronic.Domain.Common;

namespace ErpMetronic.Domain.Entities;

/// <summary>
/// Dokumen Pengeluaran/Pengiriman Barang (ke pelanggan). Saat dibuat, setiap baris
/// langsung diposting sebagai pergerakan Stok Keluar dari gudang sumber (divalidasi saldo).
/// </summary>
public class DeliveryOrder : BaseEntity
{
    [Required, StringLength(40)]
    public string ReferenceNumber { get; set; } = string.Empty;

    public DateTime DeliveryDate { get; set; } = DateTime.Today;

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    /// <summary>Sales Order sumber (kosong bila pengeluaran langsung tanpa SO).</summary>
    public int? SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }

    [StringLength(300)]
    public string? Note { get; set; }

    public ICollection<DeliveryOrderLine> Lines { get; set; } = new List<DeliveryOrderLine>();
}

public class DeliveryOrderLine : BaseEntity
{
    public int DeliveryOrderId { get; set; }
    public DeliveryOrder? DeliveryOrder { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }
}
