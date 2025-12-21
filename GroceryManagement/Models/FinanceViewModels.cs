namespace GroceryManagement.Models;

public class FinanceInvoicesVM
{
    public List<Checkout> Checkouts { get; set; } = [];
    public List<ProcurementRecord> Procurements { get; set; } = [];
}

public class FinanceReportVM
{
    public decimal Revenue { get; set; }
    public decimal ExpenseTotal { get; set; }
    public decimal ProcurementCost { get; set; }
    public decimal NetProfit { get; set; }
}

public class FinanceInvoiceDetailVM
{
    public string InvoiceType { get; set; }
    public Checkout? Checkout { get; set; }
    public ProcurementRecord? Procurement { get; set; }
}
