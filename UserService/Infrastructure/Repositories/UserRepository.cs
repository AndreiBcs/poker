using Microsoft.EntityFrameworkCore;
using UserService.Domain;
using UserService.Domain.Interfaces;
using UserService.Infrastructure.Persistence;

namespace UserService.Infrastructure.Repositories
{
    public class UserRepository(AppDbContext db) : IUserRepository
    {
        public async Task<User?> GetUserByEmail(string email) =>
            await db.Users.FirstOrDefaultAsync(u => u.Email == email);

        public async Task<User?> GetUserById(Guid id) =>
            await db.Users.FindAsync(id);

        public async Task<User?> GetUserByUsername(string username) =>
            await db.Users.FirstOrDefaultAsync(u => u.Username == username);

        public async Task<User> CreateUser(User user)
        {
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return user;
        }

        public async Task<User> UpdateUser(User user)
        {
            db.Users.Update(user);
            await db.SaveChangesAsync();
            return user;
        }

        public async Task<bool> DeleteUser(Guid id)
        {
            var user = await db.Users.FindAsync(id);
            if(user is null) return false;

            db.Remove(user);
            await db.SaveChangesAsync();
            return true;
        }

    }
}