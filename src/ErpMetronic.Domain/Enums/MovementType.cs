namespace ErpMetronic.Domain.Enums;

/// <summary>Jenis pergerakan stok.</summary>
public enum MovementType
{
    StockIn = 1,     // Barang masuk
    StockOut = 2,    // Barang keluar
    Transfer = 3,    // Pindah antar gudang
    Adjustment = 4   // Penyesuaian (stock opname)
}
