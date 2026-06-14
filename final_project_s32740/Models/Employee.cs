
namespace final_project_s32740.Models;

public class Employee
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // "Admin" or "User"
    public string? RefreshToken { get; set; }
    public DateTime RefreshTokenExpiresAt { get; set; }
}