using final_project_s32740.Dtos;
using final_project_s32740.Exceptions;
using final_project_s32740.Infrastructure;
using final_project_s32740.Models;
using final_project_s32740.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace final_project_s32740.Tests;

public class ClientServiceTests
{
    private static AppDbContext CreateDb(string name)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new AppDbContext(options);
    }


    [Fact]
    public async Task CreateIndividualClient_ValidData_CreatesSuccessfully()
    {
        var db = CreateDb("create_individual");
        var service = new ClientService(db);

        var result = await service.CreateIndividualClientAsync(new CreateIndividualClientDto
        {
            FirstName = "Anna",
            LastName = "Nowak",
            Address = "ul. Kwiatowa 5",
            Email = "anna@email.pl",
            Phone = "500600700",
            PESEL = "98102012345"
        });

        result.Id.Should().BeGreaterThan(0);
        result.FirstName.Should().Be("Anna");
        result.PESEL.Should().Be("98102012345");
    }

    [Fact]
    public async Task CreateIndividualClient_DuplicatePesel_ThrowsDomainException()
    {
        var db = CreateDb("duplicate_pesel");
        db.IndividualClients.Add(new IndividualClient
        {
            FirstName = "Istniejący",
            LastName = "Klient",
            Address = "ul. 1",
            Email = "x@y.pl",
            Phone = "111",
            PESEL = "11111111111"
        });
        await db.SaveChangesAsync();

        var service = new ClientService(db);

        await service.Invoking(s => s.CreateIndividualClientAsync(new CreateIndividualClientDto
            {
                FirstName = "Nowy",
                LastName = "Klient",
                Address = "ul. 2",
                Email = "z@y.pl",
                Phone = "222",
                PESEL = "11111111111"
            }))
            .Should().ThrowAsync<DomainException>()
            .WithMessage("*PESEL*");
    }

    [Fact]
    public async Task CreateCorporateClient_DuplicateKrs_ThrowsDomainException()
    {
        var db = CreateDb("duplicate_krs");
        db.CorporateClients.Add(new CorporateClient
        {
            CompanyName = "ACME",
            Address = "ul. A",
            Email = "acme@acme.pl",
            Phone = "123",
            KRS = "0000000001"
        });
        await db.SaveChangesAsync();

        var service = new ClientService(db);

        await service.Invoking(s => s.CreateCorporateClientAsync(new CreateCorporateClientDto
            {
                CompanyName = "Clone",
                Address = "ul. B",
                Email = "clone@clone.pl",
                Phone = "456",
                KRS = "0000000001"
            }))
            .Should().ThrowAsync<DomainException>()
            .WithMessage("*KRS*");
    }


    [Fact]
    public async Task UpdateIndividualClient_CannotChangePesel_PeselRemainsOriginal()
    {
        var db = CreateDb("update_individual");
        var client = new IndividualClient
        {
            FirstName = "Piotr",
            LastName = "Wiśniewski",
            Address = "ul. Leśna 3",
            Email = "p@w.pl",
            Phone = "600700800",
            PESEL = "80010112345"
        };
        db.IndividualClients.Add(client);
        await db.SaveChangesAsync();

        var service = new ClientService(db);

        var result = await service.UpdateIndividualClientAsync(client.Id, new UpdateIndividualClientDto
        {
            FirstName = "Przemek",
            LastName = "Wiśniewski",
            Address = "ul. Nowa 1",
            Email = "przemek@w.pl",
            Phone = "111222333"
        });

        result.FirstName.Should().Be("Przemek");
        result.PESEL.Should().Be("80010112345");
    }


    [Fact]
    public async Task DeleteIndividualClient_SoftDelete_AnonymizesData()
    {
        var db = CreateDb("soft_delete");
        var client = new IndividualClient
        {
            FirstName = "Marta",
            LastName = "Kowalska",
            Address = "ul. Zielona 7",
            Email = "marta@mail.pl",
            Phone = "700800900",
            PESEL = "90120112345"
        };
        db.IndividualClients.Add(client);
        await db.SaveChangesAsync();

        var service = new ClientService(db);

        await service.DeleteIndividualClientAsync(client.Id);

        var deleted = await db.IndividualClients.FindAsync(client.Id);
        deleted.Should().NotBeNull();
        deleted!.IsDeleted.Should().BeTrue();
        deleted.FirstName.Should().Be("DELETED");
        deleted.Email.Should().Contain("deleted");
    }

    [Fact]
    public async Task DeleteIndividualClient_AlreadyDeleted_ThrowsDomainException()
    {
        var db = CreateDb("already_deleted");
        var client = new IndividualClient
        {
            FirstName = "DELETED",
            LastName = "DELETED",
            Address = "DELETED",
            Email = "deleted_1@deleted.invalid",
            Phone = "DELETED",
            PESEL = "00000000001",
            IsDeleted = true
        };
        db.IndividualClients.Add(client);
        await db.SaveChangesAsync();

        var service = new ClientService(db);

        await service.Invoking(s => s.DeleteIndividualClientAsync(client.Id))
            .Should().ThrowAsync<DomainException>()
            .WithMessage("*już usunięty*");
    }

    [Fact]
    public async Task UpdateIndividualClient_DeletedClient_ThrowsDomainException()
    {
        var db = CreateDb("update_deleted");
        var client = new IndividualClient
        {
            FirstName = "DELETED", LastName = "DELETED", Address = "DELETED",
            Email = "d@d.invalid", Phone = "DELETED", PESEL = "11223344556",
            IsDeleted = true
        };
        db.IndividualClients.Add(client);
        await db.SaveChangesAsync();

        var service = new ClientService(db);

        await service.Invoking(s => s.UpdateIndividualClientAsync(client.Id, new UpdateIndividualClientDto
            {
                FirstName = "Jan", LastName = "X", Address = "Y", Email = "a@b.pl", Phone = "0"
            }))
            .Should().ThrowAsync<DomainException>()
            .WithMessage("*usuniętego*");
    }
}