using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MigraineDiary.Api.Auth;
using MigraineDiary.Api.Contracts.Auth;
using MigraineDiary.Api.Data;
using MigraineDiary.Api.Entities;
using MigraineDiary.Api.Exceptions;

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

        var emailExists =
            await _dbContext.Users
                .AnyAsync(
                    user => user.Email == email,
                    cancellationToken);

        if (emailExists)
        {
            throw new ConflictException(
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

        var user =
            await _dbContext.Users
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
        var user =
            await _dbContext.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.Id == userId,
                    cancellationToken);

        if (user is null)
        {
            return null;
        }

        return ToMeResponse(user);
    }

    public async Task<MeResponse?> UpdateProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var user =
            await _dbContext.Users
                .SingleOrDefaultAsync(
                    x => x.Id == userId,
                    cancellationToken);

        if (user is null)
        {
            return null;
        }

        var name = request.Name.Trim();

        var email = request.Email
            .Trim()
            .ToLowerInvariant();

        var emailExists =
            await _dbContext.Users
                .AnyAsync(
                    x =>
                        x.Id != userId &&
                        x.Email == email,
                    cancellationToken);

        if (emailExists)
        {
            throw new ConflictException(
                "Пользователь с таким email уже существует.");
        }

        user.Name = name;
        user.Email = email;

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return ToMeResponse(user);
    }

    public async Task<bool> ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user =
            await _dbContext.Users
                .SingleOrDefaultAsync(
                    x => x.Id == userId,
                    cancellationToken);

        if (user is null)
        {
            return false;
        }

        EnsurePasswordIsValid(
            user,
            request.CurrentPassword);

        user.PasswordHash =
            _passwordHasher.HashPassword(
                user,
                request.NewPassword);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAccountAsync(
        Guid userId,
        DeleteAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var user =
            await _dbContext.Users
                .SingleOrDefaultAsync(
                    x => x.Id == userId,
                    cancellationToken);

        if (user is null)
        {
            return false;
        }

        EnsurePasswordIsValid(
            user,
            request.Password);

        await using var transaction =
            await _dbContext.Database
                .BeginTransactionAsync(
                    cancellationToken);

        var attackMedicationLinks =
            await _dbContext.AttackMedications
                .Where(
                    x =>
                        x.MigraineAttack.UserId == userId ||
                        x.Medication.UserId == userId)
                .ToListAsync(
                    cancellationToken);

        _dbContext.AttackMedications.RemoveRange(
            attackMedicationLinks);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        _dbContext.Users.Remove(user);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        return true;
    }

    private void EnsurePasswordIsValid(
        User user,
        string password)
    {
        var verificationResult =
            _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                password);

        if (verificationResult ==
            PasswordVerificationResult.Failed)
        {
            throw new ArgumentException(
                "Текущий пароль указан неверно.");
        }
    }

    private static MeResponse ToMeResponse(
        User user)
    {
        return new MeResponse
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            CreatedAt = user.CreatedAt
        };
    }
}
