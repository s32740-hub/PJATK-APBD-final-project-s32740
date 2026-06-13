using final_project_s32740.Dtos;
using final_project_s32740.Exceptions;
using final_project_s32740.Infrastructure;
using final_project_s32740.Models;
using final_project_s32740.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace final_project_s32740.Tests;

public class ContractServiceTests
{
    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<(AppDbContext db, IndividualClient client, Software software)> SeedBasicDataAsync(string dbName)
    {
        var db = CreateDb(dbName);

        var software = new Software
        {
            Name = "TestApp",
            Description = "Test",
            CurrentVersion = "1.0",
            Category = "Test",
            AnnualLicensePrice = 10_000m
        };
        db.Software.Add(software);

        var client = new IndividualClient
        {
            FirstName = "Jan",
            LastName = "Kowalski",
            Address = "ul. Testowa 1",
            Email = "jan@test.pl",
            Phone = "123456789",
            PESEL = "12345678901"
        };
        db.IndividualClients.Add(client);

        await db.SaveChangesAsync();
        return (db, client, software);
    }


    [Fact]
    public async Task CreateContract_NoDiscounts_PriceEqualsAnnualLicense()
    {
        var (db, client, software) = await SeedBasicDataAsync("test_no_discounts");
        var service = new ContractService(db);

        var dto = new CreateContractDto
        {
            ClientId = client.Id,
            SoftwareId = software.Id,
            DurationDays = 10,
            AdditionalSupportYears = 0
        };

        var result = await service.CreateContractAsync(dto);

        result.TotalPrice.Should().Be(10_000m);
        result.IsSigned.Should().BeFalse();
    }

    [Fact]
    public async Task CreateContract_WithAdditionalSupportYears_IncreasesPrice()
    {
        var (db, client, software) = await SeedBasicDataAsync("test_support_years");
        var service = new ContractService(db);

        var result = await service.CreateContractAsync(new CreateContractDto
        {
            ClientId = client.Id,
            SoftwareId = software.Id,
            DurationDays = 10,
            AdditionalSupportYears = 2
        });

        result.TotalPrice.Should().Be(12_000m);
    }

    [Fact]
    public async Task CreateContract_WithActiveDiscount_AppliesDiscount()
    {
        var (db, client, software) = await SeedBasicDataAsync("test_discount");

        db.Discounts.Add(new Discount
        {
            Name = "Summer 20%",
            Type = DiscountType.Contract,
            DiscountPercent = 20m,
            StartDate = DateTime.UtcNow.Date.AddDays(-1),
            EndDate = DateTime.UtcNow.Date.AddDays(1),
            SoftwareId = null
        });
        await db.SaveChangesAsync();

        var service = new ContractService(db);

        var result = await service.CreateContractAsync(new CreateContractDto
        {
            ClientId = client.Id,
            SoftwareId = software.Id,
            DurationDays = 10,
            AdditionalSupportYears = 0
        });

        result.TotalPrice.Should().Be(8_000m);
    }

    [Fact]
    public async Task CreateContract_ReturningClient_Gets5PercentExtra()
    {
        var (db, client, software) = await SeedBasicDataAsync("test_returning_client");

        var software2 = new Software
        {
            Name = "OtherApp",
            Description = "Inny",
            CurrentVersion = "2.0",
            Category = "Finanse",
            AnnualLicensePrice = 5_000m
        };
        db.Software.Add(software2);

        var existingContract = new Contract
        {
            ClientId = client.Id,
            SoftwareId = software2.Id,
            SoftwareVersion = "2.0",
            StartDate = DateTime.UtcNow.Date.AddDays(-10),
            EndDate = DateTime.UtcNow.Date.AddDays(-1),
            TotalPrice = 5_000m,
            AnnualLicensePriceSnapshot = 5_000m,
            AdditionalSupportYears = 0,
            IsSigned = true,
            IsActive = true
        };
        db.Contracts.Add(existingContract);
        await db.SaveChangesAsync();

        var service = new ContractService(db);

        var result = await service.CreateContractAsync(new CreateContractDto
        {
            ClientId = client.Id,
            SoftwareId = software.Id,
            DurationDays = 10,
            AdditionalSupportYears = 0
        });

        result.TotalPrice.Should().Be(9_500m);
    }

    [Fact]
    public async Task CreateContract_MultipleDiscounts_TakesHighest()
    {
        var (db, client, software) = await SeedBasicDataAsync("test_max_discount");

        db.Discounts.AddRange(
            new Discount { Name = "D1", Type = DiscountType.Contract, DiscountPercent = 10m, StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(1) },
            new Discount { Name = "D2", Type = DiscountType.Contract, DiscountPercent = 25m, StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(1) },
            new Discount { Name = "D3", Type = DiscountType.Contract, DiscountPercent = 5m, StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(1) }
        );
        await db.SaveChangesAsync();

        var service = new ContractService(db);

        var result = await service.CreateContractAsync(new CreateContractDto
        {
            ClientId = client.Id,
            SoftwareId = software.Id,
            DurationDays = 10,
            AdditionalSupportYears = 0
        });

        result.TotalPrice.Should().Be(7_500m);
    }

    [Fact]
    public async Task CreateContract_AlreadyHasActiveContract_ThrowsDomainException()
    {
        var (db, client, software) = await SeedBasicDataAsync("test_duplicate_contract");

        db.Contracts.Add(new Contract
        {
            ClientId = client.Id,
            SoftwareId = software.Id,
            SoftwareVersion = "1.0",
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(10),
            TotalPrice = 10_000m,
            AnnualLicensePriceSnapshot = 10_000m,
            AdditionalSupportYears = 0,
            IsSigned = false,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var service = new ContractService(db);

        await service.Invoking(s => s.CreateContractAsync(new CreateContractDto
            {
                ClientId = client.Id,
                SoftwareId = software.Id,
                DurationDays = 10
            }))
            .Should().ThrowAsync<DomainException>()
            .WithMessage("*aktywną*umowę*");
    }

    [Fact]
    public async Task CreateContract_DeletedClient_ThrowsDomainException()
    {
        var db = CreateDb("test_deleted_client");

        var software = new Software { Name = "App", Description = "", CurrentVersion = "1.0", Category = "Test", AnnualLicensePrice = 5_000m };
        db.Software.Add(software);

        var client = new IndividualClient
        {
            FirstName = "DELETED", LastName = "DELETED", Address = "DELETED",
            Email = "d@d.invalid", Phone = "DELETED", PESEL = "99999999999", IsDeleted = true
        };
        db.IndividualClients.Add(client);
        await db.SaveChangesAsync();

        var service = new ContractService(db);

        await service.Invoking(s => s.CreateContractAsync(new CreateContractDto
            {
                ClientId = client.Id,
                SoftwareId = software.Id,
                DurationDays = 10
            }))
            .Should().ThrowAsync<DomainException>()
            .WithMessage("*usunięty*");
    }


    [Fact]
    public async Task AddPayment_FullPaymentInOneInstalment_SignsContract()
    {
        var (db, client, software) = await SeedBasicDataAsync("test_full_payment");

        var contract = new Contract
        {
            ClientId = client.Id,
            SoftwareId = software.Id,
            SoftwareVersion = "1.0",
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(10),
            TotalPrice = 10_000m,
            AnnualLicensePriceSnapshot = 10_000m,
            AdditionalSupportYears = 0,
            IsSigned = false,
            IsActive = true
        };
        db.Contracts.Add(contract);
        await db.SaveChangesAsync();

        var service = new ContractService(db);

        var result = await service.AddPaymentAsync(new CreateContractPaymentDto
        {
            ClientId = client.Id,
            ContractId = contract.Id,
            Amount = 10_000m
        });

        result.ContractFullyPaid.Should().BeTrue();
        result.TotalPaidSoFar.Should().Be(10_000m);
    }

    [Fact]
    public async Task AddPayment_Instalments_SignsWhenFull()
    {
        var (db, client, software) = await SeedBasicDataAsync("test_instalments");

        var contract = new Contract
        {
            ClientId = client.Id,
            SoftwareId = software.Id,
            SoftwareVersion = "1.0",
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(10),
            TotalPrice = 9_000m,
            AnnualLicensePriceSnapshot = 9_000m,
            AdditionalSupportYears = 0,
            IsSigned = false,
            IsActive = true
        };
        db.Contracts.Add(contract);
        await db.SaveChangesAsync();

        var service = new ContractService(db);

        var r1 = await service.AddPaymentAsync(new CreateContractPaymentDto { ClientId = client.Id, ContractId = contract.Id, Amount = 4_000m });
        r1.ContractFullyPaid.Should().BeFalse();

        var r2 = await service.AddPaymentAsync(new CreateContractPaymentDto { ClientId = client.Id, ContractId = contract.Id, Amount = 5_000m });
        r2.ContractFullyPaid.Should().BeTrue();
    }

    [Fact]
    public async Task AddPayment_OverpaymentThrowsDomainException()
    {
        var (db, client, software) = await SeedBasicDataAsync("test_overpayment");

        var contract = new Contract
        {
            ClientId = client.Id,
            SoftwareId = software.Id,
            SoftwareVersion = "1.0",
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(10),
            TotalPrice = 5_000m,
            AnnualLicensePriceSnapshot = 5_000m,
            AdditionalSupportYears = 0,
            IsSigned = false,
            IsActive = true
        };
        db.Contracts.Add(contract);
        await db.SaveChangesAsync();

        var service = new ContractService(db);

        await service.Invoking(s => s.AddPaymentAsync(new CreateContractPaymentDto
            {
                ClientId = client.Id,
                ContractId = contract.Id,
                Amount = 9_999m
            }))
            .Should().ThrowAsync<DomainException>()
            .WithMessage("*przekracza*");
    }

    [Fact]
    public async Task DeleteContract_SignedContract_ThrowsDomainException()
    {
        var (db, client, software) = await SeedBasicDataAsync("test_delete_signed");

        var contract = new Contract
        {
            ClientId = client.Id,
            SoftwareId = software.Id,
            SoftwareVersion = "1.0",
            StartDate = DateTime.UtcNow.Date.AddDays(-5),
            EndDate = DateTime.UtcNow.Date.AddDays(5),
            TotalPrice = 10_000m,
            AnnualLicensePriceSnapshot = 10_000m,
            AdditionalSupportYears = 0,
            IsSigned = true,
            IsActive = true
        };
        db.Contracts.Add(contract);
        await db.SaveChangesAsync();

        var service = new ContractService(db);

        await service.Invoking(s => s.DeleteContractAsync(contract.Id))
            .Should().ThrowAsync<DomainException>()
            .WithMessage("*podpisanego*");
    }
}