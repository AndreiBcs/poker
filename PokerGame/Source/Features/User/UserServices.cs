using Microsoft.EntityFrameworkCore;
using Source.Infrastructure.Persistence;

namespace Source.Features.User
{
    public class UserServices(AppDbContext db)
    {

        public async Task<Domain.Entities.User?> GetUserByIdAsync(Guid userId)
        {
            var user = await db.Users.FindAsync(userId);

            return user;
        }



        public async Task<Domain.Entities.User?> CreateUserAsync(UserCreateRequest dto)
        {
            if(string.IsNullOrWhiteSpace(dto.Username) 
                || string.IsNullOrWhiteSpace(dto.Email) 
                || string.IsNullOrWhiteSpace(dto.Password))
            {
                return null;
            }

            await using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                var user = new Domain.Entities.User
                {
                    Id = Guid.NewGuid(),
                    Username = dto.Username,
                    Email = dto.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
                };

                await db.Users.AddAsync(user);
                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                return user;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return null;
            }
        }



        public async Task<Domain.Entities.User?> ValidateUserCredentialsAsync(string email, string password)
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user is null) return null;

            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) return null;

            return user;
        }

        public async Task<bool> UpdateUserEmailAsync(Guid userId, string newEmail)
        {
            // Simulate updating user email
            await Task.Delay(100); // Simulate async operation
            return true; // Assume update is successful
        }



        public async Task<bool> UpdateUserPasswordAsync(Guid userId, string newPassword)
        {
            // Simulate updating user password
            await Task.Delay(100); // Simulate async operation
            return true; // Assume update is successful
        }



        public async Task<bool> UpdateUsernameAsync(Guid userId, string newUsername)
        {
            // Simulate updating username
            await Task.Delay(100); // Simulate async operation
            return true; // Assume update is successful
        }



        public async Task<bool> DeleteUserAsync(Guid userId)
        {
            await using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                var user = await db.Users.FindAsync(userId);

                if (user is null)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                db.Users.Remove(user);
                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch(Exception ex)
            {
                await transaction.RollbackAsync();

                return false;
            }
        }
    }
}