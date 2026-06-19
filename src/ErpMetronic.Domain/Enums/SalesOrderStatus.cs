namespace ErpMetronic.Domain.Enums;

/// <summary>Status siklus hidup Sales Order.</summary>
public enum SalesOrderStatus
{
    Draft = 0,
    Confirmed = 1,
    PartiallyDelivered = 2,
    Delivered = 3,
    Cancelled = 4
}

/// <summary>Status pembayaran faktur penjualan (piutang).</summary>
public enum SalesInvoiceStatus
{
    Unpaid = 0,
    PartiallyPaid = 1,
    Paid = 2
}
