using Microsoft.Extensions.DependencyInjection;
using GameService.Core;
using GameService.Core.GameLogic;
using GameService.State.Interfaces;
using GameService.Providers;
using GameService.State;

namespace GameService.Dependencies
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddGameServices(this IServiceCollection services)
        {
           //Vrem mereeeu o singura instanta de aia e singleton adica e imutabil si constructorul e privat
            services.AddSingleton<IGameStateMachine, IGameStateMachine>(); 

        
           
            services.AddTransient<IGameStateMachine, TexasHoldemStateMachine>();
            services.AddTransient<IGameStateMachine, FiveCardDrawStateMachine>();
            services.AddTransient<IHandEvaluator, HandEvaluator>();
            services.AddTransient<ITableStateDtoProvider, TableStateDtoProvider>();

            return services;
        }
        
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            // mai incolo cand bagam baza de date 
            return services;
        }
    }
}