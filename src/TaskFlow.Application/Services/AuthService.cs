using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Common;
using TaskFlow.Application.Dtos;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Services;

public class AuthService(IAppDbContext db, IPasswordHasher hasher, ITokenService tokens)
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var email = Normalize(request.Email);

        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            throw new ConflictException("Ya existe un usuario con ese correo.");

        var user = new User { Email = email, PasswordHash = hasher.Hash(request.Password) };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return ToResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = Normalize(request.Email);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user is null || !hasher.Verify(request.Password, user.PasswordHash))
            throw new InvalidCredentialsException("Credenciales inválidas.");

        return ToResponse(user);
    }

    private AuthResponse ToResponse(User user)
    {
        var (token, expiresAt) = tokens.CreateToken(user);
        return new AuthResponse(token, expiresAt);
    }

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
