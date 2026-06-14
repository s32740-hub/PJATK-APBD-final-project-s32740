using System.ComponentModel.DataAnnotations;

namespace final_project_s32740.Dtos;

public class SignInRequest
{
    [Required] public string Login { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

public class SignUpRequest
{
    [Required] public string Login { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
}