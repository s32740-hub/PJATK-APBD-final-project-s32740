using final_project_s32740.Dtos;
using final_project_s32740.Exceptions;
using final_project_s32740.Infrastructure;
using final_project_s32740.Models;
using Microsoft.EntityFrameworkCore;

namespace final_project_s32740.Services;

public class ContractService(AppDbContext db) : IContractService
{
    public async Task<ContractResponseDto> CreateContractAsync(CreateContractDto dto)
    {
        var client = await db.Clients.FindAsync(dto.ClientId)
                     ?? throw new KeyNotFoundException($"Klient o id={dto.ClientId} nie istnieje.");

        if (client is IndividualClient ind && ind.IsDeleted)
            throw new DomainException("Klient został usunięty.");
        var software = await db.Software.FindAsync(dto.SoftwareId)
                       ?? throw new KeyNotFoundException($"Oprogramowanie o id={dto.SoftwareId} nie istnieje.");
        var today = DateTime.UtcNow.Date;

        bool hasActiveContract = await db.Contracts.AnyAsync(c =>
            c.ClientId == dto.ClientId &&
            c.SoftwareId == dto.SoftwareId &&
            c.IsActive &&
            (c.IsSigned || c.EndDate.Date >= today));

        if (hasActiveContract)
            throw new DomainException("Klient ma już aktywną (nieopłaconą) umowę na ten produkt.");

        bool hasActiveSubscription = await db.Subscriptions.AnyAsync(s =>
            s.ClientId == dto.ClientId &&
            s.SoftwareId == dto.SoftwareId &&
            s.IsActive);

        if (hasActiveSubscription)
            throw new DomainException("Klient ma już aktywną subskrypcję na ten produkt.");
        var startDate = today;
        var endDate = today.AddDays(dto.DurationDays);

        decimal basePrice = software.AnnualLicensePrice + dto.AdditionalSupportYears * 1000m;
        var contractDiscounts = await db.Discounts
            .Where(d => d.Type == DiscountType.Contract &&
                        (d.SoftwareId == null || d.SoftwareId == dto.SoftwareId) &&
                        d.StartDate <= today && d.EndDate >= today)
            .ToListAsync();

        decimal bestDiscountPercent = contractDiscounts.Count != 0
            ? contractDiscounts.Max(d => d.DiscountPercent)
            : 0m;
        bool isReturningClient = await IsReturningClientAsync(dto.ClientId);
        decimal returningDiscount = isReturningClient ? 5m : 0m;

        decimal totalDiscountPercent = bestDiscountPercent + returningDiscount;
        decimal totalPrice = basePrice * (1 - totalDiscountPercent / 100m);
        var contract = new Contract
        {
            ClientId = dto.ClientId,
            SoftwareId = dto.SoftwareId,
            SoftwareVersion = software.CurrentVersion,
            StartDate = startDate,
            EndDate = endDate,
            TotalPrice = totalPrice,
            AnnualLicensePriceSnapshot = software.AnnualLicensePrice,
            AdditionalSupportYears = dto.AdditionalSupportYears,
            IsSigned = false,
            IsActive = true
        };

        db.Contracts.Add(contract);
        await db.SaveChangesAsync();

        return await BuildResponseDto(contract);
    }
    
    public async Task DeleteContractAsync(int id)
    {
        var contract = await db.Contracts
                           .Include(c => c.Payments)
                           .FirstOrDefaultAsync(c => c.Id == id)
                       ?? throw new KeyNotFoundException($"Kontrakt o id={id} nie istnieje.");

        if (contract.IsSigned)
            throw new DomainException("Nie można usunąć podpisanego kontraktu.");

        contract.IsActive = false;
        await db.SaveChangesAsync();
    }
    
    public async Task<ContractPaymentResponseDto> AddPaymentAsync(CreateContractPaymentDto dto)
    {
        var today = DateTime.UtcNow.Date;

        var contract = await db.Contracts
                           .Include(c => c.Payments)
                           .FirstOrDefaultAsync(c => c.Id == dto.ContractId && c.IsActive)
                       ?? throw new KeyNotFoundException($"Aktywny kontrakt o id={dto.ContractId} nie istnieje.");
        if (contract.ClientId != dto.ClientId)
            throw new DomainException("Podany klient nie jest właścicielem tego kontraktu.");
        if (today > contract.EndDate.Date)
        { 
            contract.IsActive = false;
            await db.SaveChangesAsync();
            throw new DomainException("Termin płatności kontraktu minął. Kontrakt został anulowany. Poprzednie wpłaty zostaną zwrócone.");
        }
        if (contract.IsSigned)
            throw new DomainException("Kontrakt jest już w pełni opłacony.");
        decimal alreadyPaid = contract.Payments.Sum(p => p.Amount);
        decimal remaining = contract.TotalPrice - alreadyPaid;

        if (dto.Amount > remaining)
            throw new DomainException($"Kwota wpłaty ({dto.Amount} PLN) przekracza pozostałą do opłacenia kwotę ({remaining} PLN).");

        var payment = new ContractPayment
        {
            ContractId = contract.Id,
            Amount = dto.Amount,
            PaymentDate = DateTime.UtcNow
        };

        db.ContractPayments.Add(payment);
        alreadyPaid += dto.Amount;

        if (alreadyPaid >= contract.TotalPrice)
        {
            contract.IsSigned = true;
        }

        await db.SaveChangesAsync();

        return new ContractPaymentResponseDto
        {
            Id = payment.Id,
            ContractId = contract.Id,
            Amount = dto.Amount,
            PaymentDate = payment.PaymentDate,
            TotalPaidSoFar = alreadyPaid,
            ContractFullyPaid = contract.IsSigned
        };
    }
    
    public async Task<ContractResponseDto> GetContractAsync(int id)
    {
        var contract = await db.Contracts
                           .Include(c => c.Client)
                           .Include(c => c.Software)
                           .Include(c => c.Payments)
                           .FirstOrDefaultAsync(c => c.Id == id)
                       ?? throw new KeyNotFoundException($"Kontrakt o id={id} nie istnieje.");

        return await BuildResponseDto(contract);
    }
    private async Task<bool> IsReturningClientAsync(int clientId)
    {
        bool hasSignedContract = await db.Contracts
            .AnyAsync(c => c.ClientId == clientId && c.IsSigned);

        bool hasSubscription = await db.Subscriptions
            .AnyAsync(s => s.ClientId == clientId);

        return hasSignedContract || hasSubscription;
    }

    private async Task<ContractResponseDto> BuildResponseDto(Contract contract)
    {
        if (contract.Client == null)
            await db.Entry(contract).Reference(c => c.Client).LoadAsync();
        if (contract.Software == null)
            await db.Entry(contract).Reference(c => c.Software).LoadAsync();
        if (!contract.Payments.Any())
            await db.Entry(contract).Collection(c => c.Payments).LoadAsync();

        string clientName = contract.Client switch
        {
            IndividualClient ind => $"{ind.FirstName} {ind.LastName}",
            CorporateClient corp => corp.CompanyName,
            _ => "Unknown"
        };

        return new ContractResponseDto
        {
            Id = contract.Id,
            ClientId = contract.ClientId,
            ClientName = clientName,
            SoftwareId = contract.SoftwareId,
            SoftwareName = contract.Software?.Name ?? "",
            SoftwareVersion = contract.SoftwareVersion,
            StartDate = contract.StartDate,
            EndDate = contract.EndDate,
            TotalPrice = contract.TotalPrice,
            AdditionalSupportYears = contract.AdditionalSupportYears,
            IsSigned = contract.IsSigned,
            PaidAmount = contract.Payments.Sum(p => p.Amount)
        };
    }
}