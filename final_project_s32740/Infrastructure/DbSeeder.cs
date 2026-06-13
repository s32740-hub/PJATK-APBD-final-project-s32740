using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RevenueRecognition.API.Models;

namespace final_project_s32740.Infrastructure;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();

        if (!await db.Employees.AnyAsync())
        {
            var hasher = new PasswordHasher<object>();

            db.Employees.AddRange(
                new Employee
                {
                    Login = "admin",
                    PasswordHash = hasher.HashPassword(null!, "admin"),
                    Role = "Admin"
                },
                new Employee
                {
                    Login = "user",
                    PasswordHash = hasher.HashPassword(null!, "user"),
                    Role = "User"
                }
            );

            await db.SaveChangesAsync();
        }
    }
}