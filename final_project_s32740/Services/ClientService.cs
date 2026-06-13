using final_project_s32740.Dtos;
using final_project_s32740.Exceptions;
using final_project_s32740.Infrastructure;
using final_project_s32740.Models;
using Microsoft.EntityFrameworkCore;

namespace final_project_s32740.Services;

public class ClientService(AppDbContext db) : IClientService
{
    public async Task<IndividualClientResponseDto> CreateIndividualClientAsync(CreateIndividualClientDto dto)
    {
        if (await db.IndividualClients.AnyAsync(c => c.PESEL == dto.PESEL))
            throw new DomainException("Klient z podanym numerem PESEL już istnieje.");

        var client = new IndividualClient
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Address = dto.Address,
            Email = dto.Email,
            Phone = dto.Phone,
            PESEL = dto.PESEL
        };

        db.IndividualClients.Add(client);
        await db.SaveChangesAsync();
        return MapToDto(client);
    }

    public async Task<CorporateClientResponseDto> CreateCorporateClientAsync(CreateCorporateClientDto dto)
    {
        if (await db.CorporateClients.AnyAsync(c => c.KRS == dto.KRS))
            throw new DomainException("Firma z podanym numerem KRS już istnieje.");

        var client = new CorporateClient
        {
            CompanyName = dto.CompanyName,
            Address = dto.Address,
            Email = dto.Email,
            Phone = dto.Phone,
            KRS = dto.KRS
        };

        db.CorporateClients.Add(client);
        await db.SaveChangesAsync();
        return MapToDto(client);
    }
    
    public async Task<IndividualClientResponseDto> UpdateIndividualClientAsync(int id, UpdateIndividualClientDto dto)
    {
        var client = await db.IndividualClients.FindAsync(id)
                     ?? throw new KeyNotFoundException($"Klient indywidualny o id={id} nie istnieje.");

        if (client.IsDeleted)
            throw new DomainException("Nie można edytować usuniętego klienta.");

        client.FirstName = dto.FirstName;
        client.LastName = dto.LastName;
        client.Address = dto.Address;
        client.Email = dto.Email;
        client.Phone = dto.Phone;
        await db.SaveChangesAsync();
        return MapToDto(client);
    }

    public async Task<CorporateClientResponseDto> UpdateCorporateClientAsync(int id, UpdateCorporateClientDto dto)
    {
        var client = await db.CorporateClients.FindAsync(id)
                     ?? throw new KeyNotFoundException($"Firma o id={id} nie istnieje.");

        client.CompanyName = dto.CompanyName;
        client.Address = dto.Address;
        client.Email = dto.Email;
        client.Phone = dto.Phone;
        // KRS nie może być zmieniony

        await db.SaveChangesAsync();
        return MapToDto(client);
    }
    
    public async Task DeleteIndividualClientAsync(int id)
    {
        var client = await db.IndividualClients.FindAsync(id)
                     ?? throw new KeyNotFoundException($"Klient indywidualny o id={id} nie istnieje.");

        if (client.IsDeleted)
            throw new DomainException("Klient jest już usunięty.");

        client.IsDeleted = true;
        client.FirstName = "DELETED";
        client.LastName = "DELETED";
        client.Address = "DELETED";
        client.Email = $"deleted_{id}@deleted.invalid";
        client.Phone = "DELETED";
        await db.SaveChangesAsync();
    }
    
    private static IndividualClientResponseDto MapToDto(IndividualClient c) => new()
    {
        Id = c.Id,
        FirstName = c.FirstName,
        LastName = c.LastName,
        Address = c.Address,
        Email = c.Email,
        Phone = c.Phone,
        PESEL = c.PESEL
    };

    private static CorporateClientResponseDto MapToDto(CorporateClient c) => new()
    {
        Id = c.Id,
        CompanyName = c.CompanyName,
        Address = c.Address,
        Email = c.Email,
        Phone = c.Phone,
        KRS = c.KRS
    };
}