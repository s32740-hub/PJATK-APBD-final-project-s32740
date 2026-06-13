using final_project_s32740.Dtos;
using final_project_s32740.Infrastructure;
using final_project_s32740.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RevenueRecognition.API.Models;

namespace final_project_s32740.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController(
    AppDbContext db,
    ITokenService tokenService,
    IPasswordService passwordService) : ControllerBase
{
    // POST /auth/sign-in
    [HttpPost("sign-in")]
    public async Task<IActionResult> SignIn([FromBody] SignInRequest request)
    {
        var employee = await db.Employees
            .FirstOrDefaultAsync(e => e.Login == request.Login.Trim().ToLowerInvariant());

        if (employee is null || !passwordService.VerifyHashedPassword(employee.PasswordHash, request.Password))
            return Unauthorized("Nieprawidłowy login lub hasło.");

        var accessToken = tokenService.GenerateAccessToken(employee.Id.ToString(), employee.Login, employee.Role);
        var refreshToken = tokenService.GenerateRefreshToken();

        employee.RefreshToken = refreshToken;
        employee.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
        await db.SaveChangesAsync();

        AppendRefreshTokenCookie(refreshToken);
        return Ok(new { accessToken });
    }

    // POST /auth/sign-up
    [HttpPost("sign-up")]
    public async Task<IActionResult> SignUp([FromBody] SignUpRequest request)
    {
        var login = request.Login.Trim().ToLowerInvariant();

        if (await db.Employees.AnyAsync(e => e.Login == login))
            return Conflict("Login jest już zajęty.");

        var employee = new Employee
        {
            Login = login,
            PasswordHash = passwordService.HashPassword(request.Password),
            Role = request.Role is "Admin" or "User" ? request.Role : "User"
        };

        var accessToken = tokenService.GenerateAccessToken(employee.Id.ToString(), employee.Login, employee.Role);
        var refreshToken = tokenService.GenerateRefreshToken();

        employee.RefreshToken = refreshToken;
        employee.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);

        db.Employees.Add(employee);
        await db.SaveChangesAsync();

        AppendRefreshTokenCookie(refreshToken);
        return Ok(new { accessToken });
    }

    // POST /auth/sign-out
    [HttpPost("sign-out")]
    public async Task<IActionResult> SignOut()
    {
        var refreshToken = HttpContext.Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized("Brak refresh tokenu.");

        var employee = await db.Employees.FirstOrDefaultAsync(e => e.RefreshToken == refreshToken);
        if (employee is null)
            return Unauthorized("Nieprawidłowy refresh token.");

        employee.RefreshToken = null;
        await db.SaveChangesAsync();

        RemoveRefreshTokenCookie();
        return Ok();
    }

    // POST /auth/refresh
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = HttpContext.Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized("Brak refresh tokenu.");

        var employee = await db.Employees
            .FirstOrDefaultAsync(e => e.RefreshToken == refreshToken && e.RefreshTokenExpiresAt >= DateTime.UtcNow);

        if (employee is null)
            return Unauthorized("Nieprawidłowy lub wygasły refresh token.");

        var accessToken = tokenService.GenerateAccessToken(employee.Id.ToString(), employee.Login, employee.Role);
        var newRefreshToken = tokenService.GenerateRefreshToken();

        employee.RefreshToken = newRefreshToken;
        employee.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
        await db.SaveChangesAsync();

        AppendRefreshTokenCookie(newRefreshToken);
        return Ok(new { accessToken });
    }

    private void AppendRefreshTokenCookie(string refreshToken)
    {
        HttpContext.Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            Expires = DateTime.UtcNow.AddDays(7),
            SameSite = SameSiteMode.Strict
        });
    }

    private void RemoveRefreshTokenCookie()
    {
        HttpContext.Response.Cookies.Delete("refreshToken", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict
        });
    }
}