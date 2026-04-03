using Microsoft.EntityFrameworkCore;

namespace User_Microservice.Data
{
    public class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
    {
        public DbSet<Models.User> Users => Set<Models.User>();
    }
}
