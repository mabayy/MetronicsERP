namespace ErpMetronic.Domain.Constants;

/// <summary>
/// Kode dokumen bawaan sistem (object code). Dipakai aplikasi untuk merujuk konfigurasi
/// penomoran. Pengguna dapat menambah kode lain sendiri lewat master Penomoran Dokumen.
/// </summary>
public static class DocumentCodes
{
    public const string PurchaseOrder = "PO";
    public const string GoodsReceipt = "GR";
    public const string DeliveryOrder = "DO";
    public const string StockIn = "IN";
    public const string StockOut = "OUT";
    public const string StockTransfer = "TRF";
    public const string StockAdjustment = "ADJ";
    public const string PurchaseInvoice = "PINV";
    public const string PurchasePayment = "PPAY";
    public const string SalesOrder = "SO";
    public const string SalesInvoice = "SINV";
    public const string SalesPayment = "SPAY";
    public const string PurchaseRequisition = "PR";
    public const string RequestForQuotation = "RFQ";
    public const string JournalVoucher = "JV";
    public const string SalesReturn = "SRET";
    public const string PurchaseReturn = "PRET";

    /// <summary>Kode bawaan beserta nama default (untuk seeding).</summary>
    public static readonly (string Code, string Name)[] BuiltIns =
    {
        (PurchaseOrder, "Purchase Order"),
        (GoodsReceipt, "Penerimaan Barang"),
        (DeliveryOrder, "Pengeluaran Barang"),
        (StockIn, "Stok Masuk"),
        (StockOut, "Stok Keluar"),
        (StockTransfer, "Transfer Stok"),
        (StockAdjustment, "Penyesuaian Stok"),
        (PurchaseInvoice, "Faktur Pembelian"),
        (PurchasePayment, "Pembayaran Pembelian"),
        (SalesOrder, "Sales Order"),
        (SalesInvoice, "Faktur Penjualan"),
        (SalesPayment, "Pembayaran Penjualan"),
        (PurchaseRequisition, "Purchase Requisition"),
        (RequestForQuotation, "Request for Quotation"),
        (JournalVoucher, "Jurnal"),
        (SalesReturn, "Retur Penjualan"),
        (PurchaseReturn, "Retur Pembelian")
    };
}
