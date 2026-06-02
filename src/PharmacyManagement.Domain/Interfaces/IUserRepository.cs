using PharmacyManagement.Domain.Models;

namespace PharmacyManagement.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username);
    Task<bool> ValidateCredentialsAsync(string username, string password);
}
