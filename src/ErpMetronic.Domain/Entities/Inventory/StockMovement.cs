using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ErpMetronic.Domain.Common;
using ErpMetronic.Domain.Enums;

namespace ErpMetronic.Domain.Entities;

/// <summary>
/// Catatan (buku besar) setiap pergerakan stok: masuk, keluar, transfer, penyesuaian.
/// </summary>
public class StockMovement : BaseEntity
{
    [Required, StringLength(40)]
    public string ReferenceNumber { get; set; } = string.Empty;

    public DateTime MovementDate { get; set; } = DateTime.UtcNow;

    public MovementType Type { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    /// <summary>Jumlah pergerakan. Positif untuk masuk/keluar/transfer; bertanda untuk penyesuaian.</summary>
    public int Quantity { get; set; }

    /// <summary>Biaya per unit untuk pergerakan ini (masuk = biaya beli; keluar = biaya rata-rata/HPP).</summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal UnitCost { get; set; }

    /// <summary>Gudang asal/utama.</summary>
    public int WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    /// <summary>Gudang tujuan (khusus transfer).</summary>
    public int? DestinationWarehouseId { get; set; }
    public Warehouse? DestinationWarehouse { get; set; }

    [StringLength(300)]
    public string? Note { get; set; }
}
