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

        public async Task<Domain.Entities.User?> GetUserByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            return await db.Users.FirstOrDefaultAsync(user => user.Email == email);
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
                var existingUser = await db.Users.AnyAsync(user => user.Email == dto.Email);
                if (existingUser)
                {
                    await transaction.RollbackAsync();
                    return null;
                }

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
            catch (Exception)
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



        public async Task<Domain.Entities.User?> UpdateUserAsync(Guid userId, UserUpdateRequest dto)
        {
            var user = await db.Users.FindAsync(userId);

            if (user is null) return null;

            if(string.IsNullOrWhiteSpace(dto.Username) 
                || string.IsNullOrWhiteSpace(dto.Email))
            {
                return null;
            }

            await using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                user.Username = dto.Username;
                user.Email = dto.Email;
                if (!string.IsNullOrWhiteSpace(dto.Password))
                {
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
                }

                db.Users.Update(user);
                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                return user;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                
                return null;
            }
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
            catch (Exception)
            {
                await transaction.RollbackAsync();

                return false;
            }
        }
    }
}
