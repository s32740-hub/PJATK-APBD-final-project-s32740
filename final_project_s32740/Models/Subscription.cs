namespace final_project_s32740.Models;

public class Subscription
{
    public int Id { get; set; }

    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;

    public int SoftwareId { get; set; }
    public Software Software { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public int RenewalPeriodMonths { get; set; } 
    public decimal BasePricePerPeriod { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime CurrentPeriodStart { get; set; }
    public DateTime CurrentPeriodEnd { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<SubscriptionPayment> Payments { get; set; } = [];
}