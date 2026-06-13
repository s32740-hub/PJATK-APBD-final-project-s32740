using System.ComponentModel.DataAnnotations;

namespace RevenueRecognition.API.DTOs.Subscriptions;

public class CreateSubscriptionDto
{
    [Required] public int ClientId { get; set; }
    [Required] public int SoftwareId { get; set; }
    [Required] public string Name { get; set; } = string.Empty;

    /// <summary>Okres odnowienia w miesiącach (1–24).</summary>
    [Range(1, 24)] public int RenewalPeriodMonths { get; set; }
}

public class SubscriptionRenewalPaymentDto
{
    [Required] public int ClientId { get; set; }
    [Required] public int SubscriptionId { get; set; }
    [Range(0.01, double.MaxValue)] public decimal Amount { get; set; }
}

public class SubscriptionResponseDto
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public int SoftwareId { get; set; }
    public string SoftwareName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int RenewalPeriodMonths { get; set; }
    public decimal BasePricePerPeriod { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime CurrentPeriodStart { get; set; }
    public DateTime CurrentPeriodEnd { get; set; }
    public bool IsActive { get; set; }
}
