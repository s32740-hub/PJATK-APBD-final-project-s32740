using final_project_s32740.DTOs;
using final_project_s32740.Exceptions;
using final_project_s32740.Infrastructure;
using final_project_s32740.Models;
using Microsoft.EntityFrameworkCore;

namespace final_project_s32740.Services;

public class SubscriptionService(AppDbContext db) : ISubscriptionService
{

    public async Task<SubscriptionResponseDto> CreateSubscriptionAsync(CreateSubscriptionDto dto)
    {
        var today = DateTime.UtcNow.Date;

        var client = await db.Clients.FindAsync(dto.ClientId)
            ?? throw new KeyNotFoundException($"Klient o id={dto.ClientId} nie istnieje.");

        if (client is IndividualClient ind && ind.IsDeleted)
            throw new DomainException("Klient został usunięty.");

        var software = await db.Software.FindAsync(dto.SoftwareId)
            ?? throw new KeyNotFoundException($"Oprogramowanie o id={dto.SoftwareId} nie istnieje.");

        bool hasSub = await db.Subscriptions.AnyAsync(s =>
            s.ClientId == dto.ClientId &&
            s.SoftwareId == dto.SoftwareId &&
            s.IsActive);

        if (hasSub)
            throw new DomainException("Klient ma już aktywną subskrypcję na ten produkt.");

        bool hasActiveContract = await db.Contracts.AnyAsync(c =>
            c.ClientId == dto.ClientId &&
            c.SoftwareId == dto.SoftwareId &&
            c.IsActive &&
            c.EndDate.Date >= today);

        if (hasActiveContract)
            throw new DomainException("Klient ma aktywny kontrakt na ten produkt.");

        decimal basePricePerPeriod = software.AnnualLicensePrice / 12m * dto.RenewalPeriodMonths;

        var subDiscounts = await db.Discounts
            .Where(d => d.Type == DiscountType.Subscription &&
                        (d.SoftwareId == null || d.SoftwareId == dto.SoftwareId) &&
                        d.StartDate <= today && d.EndDate >= today)
            .ToListAsync();

        decimal bestDiscount = subDiscounts.Count != 0
            ? subDiscounts.Max(d => d.DiscountPercent)
            : 0m;

        bool isReturning = await IsReturningClientAsync(dto.ClientId);
        decimal loyaltyDiscount = isReturning ? 5m : 0m;

        decimal firstPeriodDiscount = bestDiscount + loyaltyDiscount;
        decimal firstPeriodPrice = basePricePerPeriod * (1 - firstPeriodDiscount / 100m);

        var periodStart = today;
        var periodEnd = today.AddMonths(dto.RenewalPeriodMonths);

        var subscription = new Subscription
        {
            ClientId = dto.ClientId,
            SoftwareId = dto.SoftwareId,
            Name = dto.Name,
            RenewalPeriodMonths = dto.RenewalPeriodMonths,
            BasePricePerPeriod = basePricePerPeriod,
            StartDate = today,
            CurrentPeriodStart = periodStart,
            CurrentPeriodEnd = periodEnd,
            IsActive = true
        };

        db.Subscriptions.Add(subscription);
        await db.SaveChangesAsync();

        db.SubscriptionPayments.Add(new SubscriptionPayment
        {
            SubscriptionId = subscription.Id,
            Amount = firstPeriodPrice,
            PaymentDate = DateTime.UtcNow,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd
        });

        await db.SaveChangesAsync();

        return MapToDto(subscription, software.Name);
    }
    
    public async Task<SubscriptionResponseDto> PayRenewalAsync(SubscriptionRenewalPaymentDto dto)
    {
        var today = DateTime.UtcNow.Date;

        var subscription = await db.Subscriptions
            .Include(s => s.Payments)
            .Include(s => s.Software)
            .FirstOrDefaultAsync(s => s.Id == dto.SubscriptionId && s.ClientId == dto.ClientId)
            ?? throw new KeyNotFoundException("Subskrypcja nie istnieje lub nie należy do tego klienta.");

        if (!subscription.IsActive)
            throw new DomainException("Subskrypcja jest już anulowana.");

        if (today < subscription.CurrentPeriodEnd.Date)
            throw new DomainException("Bieżący okres subskrypcji jeszcze nie dobiegł końca. Płatność za odnowienie jest akceptowana na początku nowego okresu.");

        if (today > subscription.CurrentPeriodEnd.Date.AddDays(7))
        {
            subscription.IsActive = false;
            await db.SaveChangesAsync();
            throw new DomainException("Subskrypcja została anulowana z powodu braku płatności w terminie.");
        }

        bool isReturning = await IsReturningClientAsync(dto.ClientId);
        decimal loyaltyDiscount = isReturning ? 5m : 0m;
        decimal expectedAmount = subscription.BasePricePerPeriod * (1 - loyaltyDiscount / 100m);

        if (Math.Abs(dto.Amount - expectedAmount) > 0.01m)
            throw new DomainException($"Nieprawidłowa kwota. Oczekiwano: {expectedAmount:F2} PLN, podano: {dto.Amount:F2} PLN.");

        var newPeriodStart = subscription.CurrentPeriodEnd;
        var newPeriodEnd = newPeriodStart.AddMonths(subscription.RenewalPeriodMonths);

        db.SubscriptionPayments.Add(new SubscriptionPayment
        {
            SubscriptionId = subscription.Id,
            Amount = dto.Amount,
            PaymentDate = DateTime.UtcNow,
            PeriodStart = newPeriodStart,
            PeriodEnd = newPeriodEnd
        });

        subscription.CurrentPeriodStart = newPeriodStart;
        subscription.CurrentPeriodEnd = newPeriodEnd;

        await db.SaveChangesAsync();

        return MapToDto(subscription, subscription.Software?.Name ?? "");
    }


    private async Task<bool> IsReturningClientAsync(int clientId)
    {
        bool hasContract = await db.Contracts.AnyAsync(c => c.ClientId == clientId && c.IsSigned);
        bool hasSub = await db.Subscriptions.AnyAsync(s => s.ClientId == clientId);
        return hasContract || hasSub;
    }

    private static SubscriptionResponseDto MapToDto(Subscription s, string softwareName) => new()
    {
        Id = s.Id,
        ClientId = s.ClientId,
        SoftwareId = s.SoftwareId,
        SoftwareName = softwareName,
        Name = s.Name,
        RenewalPeriodMonths = s.RenewalPeriodMonths,
        BasePricePerPeriod = s.BasePricePerPeriod,
        StartDate = s.StartDate,
        CurrentPeriodStart = s.CurrentPeriodStart,
        CurrentPeriodEnd = s.CurrentPeriodEnd,
        IsActive = s.IsActive
    };
}
