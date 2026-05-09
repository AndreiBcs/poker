namespace UserService.Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetUserById(Guid id);
        Task<User?> GetUserByUsername(string username);
        Task<User> CreateUser(User user);
        Task<User> UpdateUser(User user);
        Task<bool> DeleteUser(Guid id);
    }
}