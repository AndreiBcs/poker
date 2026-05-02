using Microsoft.Extensions.DependencyInjection;

namespace GameService.Dependencies
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddGameServices(this IServiceCollection services)
        {
            
            services.AddScoped<IGameManager, GameManager>(); 
            services.AddScoped<ITableStateDtoProvider, TableStateDtoProvider>(); 
            // aci se vor baga mai multe servicii(injectii de dependinte)
            // ex :services.AddScoped<ICardShuffler, CardShuffler>();
            //  ex : services.AddScoped<IPlayerActionEvaluator, PlayerActionEvaluator>();
            return services;
        }
        
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            
            services.AddScoped<IGameRepository, GameRepository>();
           
            return services;
        }

     
    }
}