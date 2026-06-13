using final_project_s32740.Exceptions;
using final_project_s32740.Infrastructure;
using final_project_s32740.Models;
using final_project_s32740.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RevenueRecognition.API.DTOs.Subscriptions;
using Xunit;

namespace final_project_s32740.Tests;

public class SubscriptionServiceTests
{
    private static AppDbContext CreateDb(string name)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<(AppDbContext db, IndividualClient client, Software software)> SeedAsync(string name)
    {
        var db = CreateDb(name);
        var software = new Software
        {
            Name = "SubApp", Description = "Test", CurrentVersion = "1.0",
            Category = "Test", AnnualLicensePrice = 12_000m
        };
        db.Software.Add(software);
        var client = new IndividualClient
        {
            FirstName = "Jan", LastName = "Kowalski", Address = "ul. 1",
            Email = "jan@test.pl", Phone = "123", PESEL = "12345678901"
        };
        db.IndividualClients.Add(client);
        await db.SaveChangesAsync();
        return (db, client, software);
    }

    [Fact]
    public async Task CreateSubscription_NoDiscounts_PriceEqualsMonthlyRate()
    {
        var (db, client, software) = await SeedAsync("sub_no_discount");
        var service = new SubscriptionService(db);

        var result = await service.CreateSubscriptionAsync(new CreateSubscriptionDto
        {
            ClientId = client.Id,
            SoftwareId = software.Id,
            Name = "Plan miesięczny",
            RenewalPeriodMonths = 1
        });
        result.BasePricePerPeriod.Should().Be(1_000m);
    }

    [Fact]
    public async Task CreateSubscription_ReturningClient_Gets5PercentLoyaltyDiscount()
    {
        var (db, client, software) = await SeedAsync("sub_loyalty");

        var software2 = new Software
        {
            Name = "Other", Description = "", CurrentVersion = "1.0",
            Category = "Test", AnnualLicensePrice = 5_000m
        };
        db.Software.Add(software2);
        db.Contracts.Add(new Contract
        {
            ClientId = client.Id, SoftwareId = software2.Id, SoftwareVersion = "1.0",
            StartDate = DateTime.UtcNow.AddDays(-10), EndDate = DateTime.UtcNow.AddDays(-1),
            TotalPrice = 5_000m, AnnualLicensePriceSnapshot = 5_000m,
            AdditionalSupportYears = 0, IsSigned = true, IsActive = true
        });
        await db.SaveChangesAsync();

        var service = new SubscriptionService(db);
        var result = await service.CreateSubscriptionAsync(new CreateSubscriptionDto
        {
            ClientId = client.Id, SoftwareId = software.Id,
            Name = "Plan", RenewalPeriodMonths = 1
        });

        var payment = await db.SubscriptionPayments
            .FirstAsync(p => p.SubscriptionId == result.Id);
        payment.Amount.Should().Be(950m);
    }

    [Fact]
    public async Task CreateSubscription_WithPromoDiscount_AppliesHighestDiscount()
    {
        var (db, client, software) = await SeedAsync("sub_promo");
        db.Discounts.Add(new Discount
        {
            Name = "Promo", Type = DiscountType.Subscription, DiscountPercent = 10m,
            StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(1)
        });
        await db.SaveChangesAsync();

        var service = new SubscriptionService(db);
        var result = await service.CreateSubscriptionAsync(new CreateSubscriptionDto
        {
            ClientId = client.Id, SoftwareId = software.Id,
            Name = "Plan", RenewalPeriodMonths = 1
        });

        var payment = await db.SubscriptionPayments.FirstAsync(p => p.SubscriptionId == result.Id);
        payment.Amount.Should().Be(900m);
    }

    [Fact]
    public async Task CreateSubscription_DuplicateActive_ThrowsDomainException()
    {
        var (db, client, software) = await SeedAsync("sub_duplicate");
        db.Subscriptions.Add(new Subscription
        {
            ClientId = client.Id, SoftwareId = software.Id, Name = "Istniejąca",
            RenewalPeriodMonths = 1, BasePricePerPeriod = 1_000m,
            StartDate = DateTime.UtcNow, CurrentPeriodStart = DateTime.UtcNow,
            CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1), IsActive = true
        });
        await db.SaveChangesAsync();

        var service = new SubscriptionService(db);
        await service.Invoking(s => s.CreateSubscriptionAsync(new CreateSubscriptionDto
            {
                ClientId = client.Id, SoftwareId = software.Id,
                Name = "Nowa", RenewalPeriodMonths = 1
            }))
            .Should().ThrowAsync<DomainException>()
            .WithMessage("*subskrypcję*");
    }

    [Fact]
    public async Task PayRenewal_CorrectAmount_UpdatesPeriod()
    {
        var (db, client, software) = await SeedAsync("sub_renew");
        var periodEnd = DateTime.UtcNow.Date;
        var sub = new Subscription
        {
            ClientId = client.Id, SoftwareId = software.Id, Name = "Plan",
            RenewalPeriodMonths = 1, BasePricePerPeriod = 1_000m,
            StartDate = DateTime.UtcNow.AddMonths(-1),
            CurrentPeriodStart = DateTime.UtcNow.AddMonths(-1),
            CurrentPeriodEnd = periodEnd, IsActive = true
        };
        db.Subscriptions.Add(sub);
        await db.SaveChangesAsync();

        var service = new SubscriptionService(db);
        var result = await service.PayRenewalAsync(new SubscriptionRenewalPaymentDto
        {
            ClientId = client.Id, SubscriptionId = sub.Id,
            Amount = 950m
        });

        result.IsActive.Should().BeTrue();
        result.CurrentPeriodEnd.Should().Be(periodEnd.AddMonths(1));
    }

    [Fact]
    public async Task PayRenewal_WrongAmount_ThrowsDomainException()
    {
        var (db, client, software) = await SeedAsync("sub_wrong_amount");
        var sub = new Subscription
        {
            ClientId = client.Id, SoftwareId = software.Id, Name = "Plan",
            RenewalPeriodMonths = 1, BasePricePerPeriod = 1_000m,
            StartDate = DateTime.UtcNow.AddMonths(-1),
            CurrentPeriodStart = DateTime.UtcNow.AddMonths(-1),
            CurrentPeriodEnd = DateTime.UtcNow.Date, IsActive = true
        };
        db.Subscriptions.Add(sub);
        await db.SaveChangesAsync();

        var service = new SubscriptionService(db);
        await service.Invoking(s => s.PayRenewalAsync(new SubscriptionRenewalPaymentDto
            {
                ClientId = client.Id, SubscriptionId = sub.Id,
                Amount = 500m // za mało
            }))
            .Should().ThrowAsync<DomainException>()
            .WithMessage("*Nieprawidłowa kwota*");
    }

    [Fact]
    public async Task PayRenewal_TooLate_CancelsSubscription()
    {
        var (db, client, software) = await SeedAsync("sub_too_late");
        var sub = new Subscription
        {
            ClientId = client.Id, SoftwareId = software.Id, Name = "Plan",
            RenewalPeriodMonths = 1, BasePricePerPeriod = 1_000m,
            StartDate = DateTime.UtcNow.AddMonths(-2),
            CurrentPeriodStart = DateTime.UtcNow.AddMonths(-2),
            CurrentPeriodEnd = DateTime.UtcNow.Date.AddDays(-8),
            IsActive = true
        };
        db.Subscriptions.Add(sub);
        await db.SaveChangesAsync();

        var service = new SubscriptionService(db);
        await service.Invoking(s => s.PayRenewalAsync(new SubscriptionRenewalPaymentDto
            {
                ClientId = client.Id, SubscriptionId = sub.Id, Amount = 950m
            }))
            .Should().ThrowAsync<DomainException>()
            .WithMessage("*anulowana*");

        var updated = await db.Subscriptions.FindAsync(sub.Id);
        updated!.IsActive.Should().BeFalse();
    }
}