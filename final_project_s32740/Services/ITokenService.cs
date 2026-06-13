namespace final_project_s32740.Services;

public interface ITokenService
{
    string GenerateAccessToken(string userId, string login, string role);
    string GenerateRefreshToken();
}