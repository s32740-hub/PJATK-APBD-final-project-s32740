using RevenueRecognition.API.DTOs.Subscriptions;

namespace final_project_s32740.Services;

public interface ISubscriptionService
{
    Task<SubscriptionResponseDto> CreateSubscriptionAsync(CreateSubscriptionDto dto);
    Task<SubscriptionResponseDto> PayRenewalAsync(SubscriptionRenewalPaymentDto dto);
}