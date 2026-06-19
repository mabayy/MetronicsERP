namespace ErpMetronic.Domain.Enums;

/// <summary>Status Purchase Requisition.</summary>
public enum PurchaseRequisitionStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    Rejected = 3
}

/// <summary>Status Request for Quotation.</summary>
public enum RequestForQuotationStatus
{
    Draft = 0,
    Sent = 1,
    Closed = 2
}
