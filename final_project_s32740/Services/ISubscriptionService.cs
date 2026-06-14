using final_project_s32740.DTOs;
namespace final_project_s32740.Services;

public interface ISubscriptionService
{
    Task<SubscriptionResponseDto> CreateSubscriptionAsync(CreateSubscriptionDto dto);
    Task<SubscriptionResponseDto> PayRenewalAsync(SubscriptionRenewalPaymentDto dto);
}