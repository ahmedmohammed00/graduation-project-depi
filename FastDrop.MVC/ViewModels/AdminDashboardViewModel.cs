namespace FastDrop.MVC.ViewModels;

public sealed class AdminDashboardViewModel
{
    public int EmployeeCount { get; init; }
    public int MerchantCount { get; init; }
    public int ActiveMerchantCount { get; init; }
    public int UnpaidInvoiceCount { get; init; }
    public decimal UnpaidInvoiceAmount { get; init; }
    public int PendingPaymentCount { get; init; }
    public decimal PendingPaymentAmount { get; init; }
    public IReadOnlyList<DashboardMerchant> Merchants { get; init; } = Array.Empty<DashboardMerchant>();
}

public sealed record DashboardMerchant(int Id, string ShopName, string? ContactName, string Email, string Status);
