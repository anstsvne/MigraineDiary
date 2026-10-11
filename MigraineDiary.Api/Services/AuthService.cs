using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MigraineDiary.Api.Auth;
using MigraineDiary.Api.Contracts.Auth;
using MigraineDiary.Api.Data;
using MigraineDiary.Api.Entities;

namespace MigraineDiary.Api.Services;

public sealed class AuthService
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly JwtTokenService _jwtTokenService;

    public AuthService(
     AppDbContext dbContext,
     IPasswordHasher<User> passwordHasher,
     JwtTokenService jwtTokenService)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<RegisterResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        var email = request.Email
            .Trim()
            .ToLowerInvariant();

        var emailExists = await _dbContext.Users
            .AnyAsync(
                user => user.Email == email,
                cancellationToken);

        if (emailExists)
        {
            throw new InvalidOperationException(
                "Пользователь с таким email уже существует.");
        }

        var user = new User
        {
            Name = name,
            Email = email,
            PasswordHash = string.Empty
        };

        user.PasswordHash =
            _passwordHasher.HashPassword(
                user,
                request.Password);

        _dbContext.Users.Add(user);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return new RegisterResponse
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            CreatedAt = user.CreatedAt
        };
    }
    public async Task<LoginResponse?> LoginAsync(
    LoginRequest request,
    CancellationToken cancellationToken = default)
    {
        var email = request.Email
            .Trim()
            .ToLowerInvariant();

        var user = await _dbContext.Users
            .SingleOrDefaultAsync(
                x => x.Email == email,
                cancellationToken);

        if (user is null)
        {
            return null;
        }

        var verificationResult =
            _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password);

        if (verificationResult ==
            PasswordVerificationResult.Failed)
        {
            return null;
        }

        var token =
            _jwtTokenService.CreateToken(user);

        return new LoginResponse
        {
            AccessToken = token.Token,
            ExpiresAt = token.ExpiresAt,
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email
        };
    }

    public async Task<MeResponse?> GetMeAsync(
    Guid userId,
    CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == userId,
                cancellationToken);

        if (user is null)
        {
            return null;
        }

        return new MeResponse
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            CreatedAt = user.CreatedAt
        };
    }
}