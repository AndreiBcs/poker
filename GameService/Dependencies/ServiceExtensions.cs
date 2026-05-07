using Microsoft.Extensions.DependencyInjection;
using GameService.Core;
using GameService.Core.GameLogic;
using GameService.State.Interfaces;
using GameService.Providers;

namespace GameService.Dependencies
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddGameServices(this IServiceCollection services)
        {
            
            services.AddSingleton<IGameManager, GameManager>(); 
            
          
            services.AddTransient<ITableStateDtoProvider, TableStateDtoProvider>(); 
            services.AddTransient<IHandEvaluator, HandEvaluator>(); 

            return services;
        }
        
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
      
            services.AddScoped<IGameRepository, GameRepository>();
            return services;
        }
    }
}