using PharmacyManagement.Domain.Enums;
using PharmacyManagement.Domain.Interfaces;
using PharmacyManagement.Domain.Models;
using PharmacyManagement.Infrastructure.Security;

namespace PharmacyManagement.Infrastructure.Services;

public interface IAuthService
{
    Task<User?> AuthenticateAsync(string username, string password);
    Task<User?> RegisterAsync(string username, string password, string fullName, UserRole role = UserRole.Pharmacist);
    Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
    Task<bool> UserExistsAsync(string username);
}

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<User?> AuthenticateAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return null;

        var user = await _unitOfWork.Users.GetByUsernameAsync(username);
        if (user == null || !user.IsActive)
            return null;

        if (!PasswordHasher.VerifyPassword(password, user.PasswordHash))
            return null;

        user.LastLoginDate = DateTime.UtcNow;
        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return user;
    }

    public async Task<User?> RegisterAsync(string username, string password, string fullName, UserRole role = UserRole.Pharmacist)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(fullName))
            return null;

        if (await _unitOfWork.Users.ExistsAsync(u => u.Username == username))
            return null;

        var user = new User
        {
            Username = username,
            PasswordHash = PasswordHasher.HashPassword(password),
            FullName = fullName,
            Role = role,
            IsActive = true
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return user;
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
            return false;

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            return false;

        if (!PasswordHasher.VerifyPassword(currentPassword, user.PasswordHash))
            return false;

        user.PasswordHash = PasswordHasher.HashPassword(newPassword);
        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UserExistsAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return false;
        return await _unitOfWork.Users.ExistsAsync(u => u.Username == username);
    }
}
