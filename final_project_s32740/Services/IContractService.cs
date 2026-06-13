using final_project_s32740.Dtos;

namespace final_project_s32740.Services;
public interface IContractService
{
    Task<ContractResponseDto> CreateContractAsync(CreateContractDto dto);
    Task DeleteContractAsync(int id);
    Task<ContractPaymentResponseDto> AddPaymentAsync(CreateContractPaymentDto dto);
    Task<ContractResponseDto> GetContractAsync(int id);
}
