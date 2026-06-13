using final_project_s32740.Dtos;
using final_project_s32740.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace final_project_s32740.Services;
public class RevenueService(AppDbContext db, IHttpClientFactory httpClientFactory) : IRevenueService
{
    public async Task<RevenueResponseDto> GetCurrentRevenueAsync(int? softwareId, string currency)
    {
        var contractQuery = db.Contracts
            .Where(c => c.IsSigned && c.IsActive);

        if (softwareId.HasValue)
            contractQuery = contractQuery.Where(c => c.SoftwareId == softwareId.Value);

        decimal contractRevenue = await contractQuery.SumAsync(c => c.TotalPrice);

        var subPaymentQuery = db.SubscriptionPayments
            .Include(p => p.Subscription)
            .AsQueryable();

        if (softwareId.HasValue)
            subPaymentQuery = subPaymentQuery.Where(p => p.Subscription.SoftwareId == softwareId.Value);

        decimal subscriptionRevenue = await subPaymentQuery.SumAsync(p => p.Amount);

        decimal totalPln = contractRevenue + subscriptionRevenue;
        return await BuildResponseAsync(totalPln, currency);
    }
    public async Task<RevenueResponseDto> GetPredictedRevenueAsync(int? softwareId, string currency)
    {
        var currentResponse = await GetCurrentRevenueAsync(softwareId, "PLN");
        decimal totalPln = currentResponse.Amount;

        var pendingContractQuery = db.Contracts
            .Include(c => c.Payments)
            .Where(c => c.IsActive && !c.IsSigned);

        if (softwareId.HasValue)
            pendingContractQuery = pendingContractQuery.Where(c => c.SoftwareId == softwareId.Value);

        var pendingContracts = await pendingContractQuery.ToListAsync();
        decimal pendingContractRevenue = pendingContracts.Sum(c =>
            c.TotalPrice - c.Payments.Sum(p => p.Amount));

        totalPln += pendingContractRevenue;
        var activeSubs = db.Subscriptions
            .Where(s => s.IsActive);

        if (softwareId.HasValue)
            activeSubs = activeSubs.Where(s => s.SoftwareId == softwareId.Value);

        decimal futureSubRevenue = await activeSubs.SumAsync(s => s.BasePricePerPeriod * 0.95m);
        totalPln += futureSubRevenue;

        return await BuildResponseAsync(totalPln, currency);
    }
    private async Task<RevenueResponseDto> BuildResponseAsync(decimal amountPln, string currency)
    {
        currency = currency.Trim().ToUpperInvariant();

        if (currency == "PLN" || string.IsNullOrEmpty(currency))
            return new RevenueResponseDto { Amount = Math.Round(amountPln, 2), Currency = "PLN" };

        decimal rate = await GetExchangeRateFromNbpAsync(currency);
        decimal converted = amountPln / rate;

        return new RevenueResponseDto
        {
            Amount = Math.Round(converted, 2),
            Currency = currency
        };
    }
    private async Task<decimal> GetExchangeRateFromNbpAsync(string currencyCode)
    {
        var client = httpClientFactory.CreateClient("NBP");

        try
        {
            var url = $"https://api.nbp.pl/api/exchangerates/rates/A/{currencyCode}/?format=json";
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Nie można pobrać kursu dla waluty '{currencyCode}'. Status: {response.StatusCode}");

            var json = await response.Content.ReadFromJsonAsync<NbpRateResponse>();
            return json?.Rates?.FirstOrDefault()?.Mid
                ?? throw new InvalidOperationException("Brak danych kursu z API NBP.");
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Błąd komunikacji z API NBP: {ex.Message}", ex);
        }
    }
    private class NbpRateResponse
    {
        public List<NbpRate>? Rates { get; set; }
    }

    private class NbpRate
    {
        public decimal Mid { get; set; }
    }
}
