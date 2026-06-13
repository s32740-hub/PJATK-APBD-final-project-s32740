namespace final_project_s32740.Models;

public abstract class Client
{
    public int Id { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    public ICollection<Contract> Contracts { get; set; } = [];
    public ICollection<Subscription> Subscriptions { get; set; } = [];
}