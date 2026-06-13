using final_project_s32740.Dtos;

namespace final_project_s32740.Services;


public interface IClientService
{
    Task<IndividualClientResponseDto> CreateIndividualClientAsync(CreateIndividualClientDto dto);
    Task<CorporateClientResponseDto> CreateCorporateClientAsync(CreateCorporateClientDto dto);
    Task<IndividualClientResponseDto> UpdateIndividualClientAsync(int id, UpdateIndividualClientDto dto);
    Task<CorporateClientResponseDto> UpdateCorporateClientAsync(int id, UpdateCorporateClientDto dto);
    Task DeleteIndividualClientAsync(int id);
}