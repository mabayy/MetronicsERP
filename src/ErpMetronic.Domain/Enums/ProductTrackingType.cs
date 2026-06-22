namespace ErpMetronic.Domain.Enums;

/// <summary>Cara pelacakan persediaan sebuah produk.</summary>
public enum ProductTrackingType
{
    None = 0,    // Tanpa pelacakan khusus
    Lot = 1,     // Per batch/lot (+ kedaluwarsa)
    Serial = 2   // Per nomor seri unit
}
