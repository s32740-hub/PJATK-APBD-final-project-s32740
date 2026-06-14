namespace final_project_s32740.Models;

public class Contract
{
    public int Id { get; set; }

    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;

    public int SoftwareId { get; set; }
    public Software Software { get; set; } = null!;

    public string SoftwareVersion { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public decimal TotalPrice { get; set; }

    public decimal AnnualLicensePriceSnapshot { get; set; }
    public int AdditionalSupportYears { get; set; } 
    public bool IsSigned { get; set; } = false;

    public bool IsActive { get; set; } = true;
    public ICollection<ContractPayment> Payments { get; set; } = [];
    public decimal PaidAmount => Payments.Sum(p => p.Amount);
    public bool IsExpiredUnpaid => !IsSigned && DateTime.UtcNow.Date > EndDate.Date;
}