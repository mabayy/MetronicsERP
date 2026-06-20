namespace ErpMetronic.Domain.Enums;

/// <summary>Jenis pajak.</summary>
public enum TaxKind
{
    /// <summary>PPN — Pajak Pertambahan Nilai (VAT). Menambah nilai dokumen.</summary>
    ValueAdded = 1,
    /// <summary>PPh — Pajak Penghasilan dipotong (withholding tax). Mengurangi nilai dibayar.</summary>
    Withholding = 2
}

/// <summary>Pada transaksi apa pajak boleh dipakai.</summary>
public enum TaxApplicability
{
    Sales = 1,
    Purchase = 2,
    Both = 3
}
