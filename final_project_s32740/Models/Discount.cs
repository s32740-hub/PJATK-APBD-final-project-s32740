namespace final_project_s32740.Models;

public enum DiscountType
{
    Contract = 0,
    Subscription = 1
}

public class Discount
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DiscountType Type { get; set; }
    public decimal DiscountPercent { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public int? SoftwareId { get; set; }
    public Software? Software { get; set; }

    public bool IsActiveOn(DateTime date) => date >= StartDate && date <= EndDate;
}