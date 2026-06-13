
namespace final_project_s32740.Models;

public class Software
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CurrentVersion { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal AnnualLicensePrice { get; set; }

    public ICollection<Contract> Contracts { get; set; } = [];
    public ICollection<Subscription> Subscriptions { get; set; } = [];
    public ICollection<Discount> Discounts { get; set; } = [];
}