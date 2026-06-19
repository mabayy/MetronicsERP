namespace ErpMetronic.Domain.Enums;

/// <summary>Status siklus hidup Purchase Order.</summary>
public enum PurchaseOrderStatus
{
    Draft = 0,              // Baru dibuat, masih bisa diubah
    Ordered = 1,            // Dikonfirmasi/dipesan ke pemasok
    PartiallyReceived = 2,  // Sebagian barang sudah diterima
    Received = 3,           // Seluruh barang diterima
    Cancelled = 4           // Dibatalkan
}
