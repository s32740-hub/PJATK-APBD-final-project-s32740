using final_project_s32740.Dtos;

namespace final_project_s32740.Services;

public interface IRevenueService
{
    Task<RevenueResponseDto> GetCurrentRevenueAsync(int? softwareId, string currency);
    Task<RevenueResponseDto> GetPredictedRevenueAsync(int? softwareId, string currency);
}