
namespace final_project_s32740.Models;

public class IndividualClient : Client
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PESEL { get; set; } = string.Empty; // Nie może być zmieniony po utworzeniu
    public bool IsDeleted { get; set; } = false;      // Miękkie usunięcie
}