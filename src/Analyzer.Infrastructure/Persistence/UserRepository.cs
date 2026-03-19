using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Domain.Entities;
using BCrypt.Net;

namespace Analyzer.Infrastructure.Persistence;

public class UserRepository : IUserRepository
{
    public async Task AddAsync(User user)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> ExistsByUsernameAsync(string username)
    {
        throw new NotImplementedException();
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        List<User> users = [
            new(
                "aboba", 
                "aboba@aboba.ru", 
                BCrypt.Net.BCrypt.EnhancedHashPassword("aboba") 
            ),

            new(
                "bobs", 
                "bobs@bobs.ru", 
                BCrypt.Net.BCrypt.EnhancedHashPassword("bobs") 
            ),

            new(
                "bobik", 
                "bosik@bobik.ru", 
                BCrypt.Net.BCrypt.EnhancedHashPassword("bobik")
            ),

            new(
                "admin", 
                "admin@admin.ru", 
                BCrypt.Net.BCrypt.EnhancedHashPassword("admin")
            ),
        ];

        return users.Find(user => user.Username == username);
    }

    public async Task<User?> GetByIdAsync(Guid userId)
    {
        throw new NotImplementedException();
    }

    public async Task UpdateAsync(User user)
    {
        throw new NotImplementedException();
    }
}