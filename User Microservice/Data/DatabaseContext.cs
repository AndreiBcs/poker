using Microsoft.EntityFrameworkCore;

namespace User_Microservice.Data
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
        {
        }
        public DbSet<Models.User> Users => Set<Models.User>();
    }
}
