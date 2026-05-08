using Microsoft.Extensions.DependencyInjection;
using GameService.Core; // For IGameManager, GameManager
// If GameRepository doesn't exist yet, we will map it temporarily to dynamic or comment it out if not implemented
// using GameService.Data; 

namespace GameService.Dependencies
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddGameServices(this IServiceCollection services)
        {
            
            return services;
        }
        
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
           
            return services;
        }
    }
}