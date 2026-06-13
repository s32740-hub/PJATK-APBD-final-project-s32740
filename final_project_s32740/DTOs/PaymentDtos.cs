using System.ComponentModel.DataAnnotations;

namespace final_project_s32740.Dtos;

public class CreateContractPaymentDto
{
    [Required] public int ClientId { get; set; }
    [Required] public int ContractId { get; set; }
    [Range(0.01, double.MaxValue)] public decimal Amount { get; set; }
}

public class ContractPaymentResponseDto
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal TotalPaidSoFar { get; set; }
    public bool ContractFullyPaid { get; set; }
}