using final_project_s32740.Models;

namespace final_project_s32740.Infrastructure;
using Microsoft.EntityFrameworkCore;
using RevenueRecognition.API.Models;
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<IndividualClient> IndividualClients => Set<IndividualClient>();
    public DbSet<CorporateClient> CorporateClients => Set<CorporateClient>();
    public DbSet<Software> Software => Set<Software>();
    public DbSet<Discount> Discounts => Set<Discount>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<ContractPayment> ContractPayments => Set<ContractPayment>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<SubscriptionPayment> SubscriptionPayments => Set<SubscriptionPayment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Client>()
            .HasDiscriminator<string>("ClientType")
            .HasValue<IndividualClient>("Individual")
            .HasValue<CorporateClient>("Corporate");

        modelBuilder.Entity<IndividualClient>()
            .HasIndex(c => c.PESEL)
            .IsUnique();

        modelBuilder.Entity<CorporateClient>()
            .HasIndex(c => c.KRS)
            .IsUnique();

        modelBuilder.Entity<Software>()
            .Property(s => s.AnnualLicensePrice)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Discount>()
            .Property(d => d.DiscountPercent)
            .HasColumnType("decimal(5,2)");

        modelBuilder.Entity<Contract>()
            .Property(c => c.TotalPrice)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Contract>()
            .Property(c => c.AnnualLicensePriceSnapshot)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Contract>()
            .Ignore(c => c.PaidAmount)
            .Ignore(c => c.IsExpiredUnpaid);

        modelBuilder.Entity<Contract>()
            .HasOne(c => c.Client)
            .WithMany(cl => cl.Contracts)
            .HasForeignKey(c => c.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Contract>()
            .HasOne(c => c.Software)
            .WithMany(s => s.Contracts)
            .HasForeignKey(c => c.SoftwareId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ContractPayment>()
            .Property(p => p.Amount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Subscription>()
            .Property(s => s.BasePricePerPeriod)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Subscription>()
            .HasOne(s => s.Client)
            .WithMany(c => c.Subscriptions)
            .HasForeignKey(s => s.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Subscription>()
            .HasOne(s => s.Software)
            .WithMany(sw => sw.Subscriptions)
            .HasForeignKey(s => s.SoftwareId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SubscriptionPayment>()
            .Property(p => p.Amount)
            .HasColumnType("decimal(18,2)");
        
        
        modelBuilder.Entity<Software>().HasData(
            new Software
            {
                Id = 1,
                Name = "FinApp Pro",
                Description = "System zarządzania finansami dla przedsiębiorstw",
                CurrentVersion = "3.2.1",
                Category = "Finanse",
                AnnualLicensePrice = 10000m
            },
            new Software
            {
                Id = 2,
                Name = "EduLearn",
                Description = "Platforma e-learningowa",
                CurrentVersion = "1.5.0",
                Category = "Edukacja",
                AnnualLicensePrice = 5000m
            }
        );

        modelBuilder.Entity<Discount>().HasData(
            new Discount
            {
                Id = 1,
                Name = "Black Friday Discount",
                Type = DiscountType.Subscription,
                DiscountPercent = 10m,
                StartDate = new DateTime(DateTime.UtcNow.Year, 1, 1),
                EndDate = new DateTime(DateTime.UtcNow.Year, 3, 3),
                SoftwareId = null
            },
            new Discount
            {
                Id = 2,
                Name = "Summer Sale",
                Type = DiscountType.Contract,
                DiscountPercent = 15m,
                StartDate = new DateTime(DateTime.UtcNow.Year, 6, 1),
                EndDate = new DateTime(DateTime.UtcNow.Year, 8, 31),
                SoftwareId = null
            }
        );
    }
}
