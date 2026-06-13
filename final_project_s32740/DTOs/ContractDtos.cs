using System.ComponentModel.DataAnnotations;

namespace final_project_s32740.Dtos;

public class CreateContractDto
{
    [Required] public int ClientId { get; set; }
    [Required] public int SoftwareId { get; set; }

    [Range(3, 30)] public int DurationDays { get; set; } = 30;

    [Range(0, 3)] public int AdditionalSupportYears { get; set; } = 0;
}

public class ContractResponseDto
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public int SoftwareId { get; set; }
    public string SoftwareName { get; set; } = string.Empty;
    public string SoftwareVersion { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalPrice { get; set; }
    public int AdditionalSupportYears { get; set; }
    public bool IsSigned { get; set; }
    public decimal PaidAmount { get; set; }
}