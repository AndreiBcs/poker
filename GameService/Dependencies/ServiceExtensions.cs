using Microsoft.Extensions.DependencyInjection;
using GameService.Core;                  // Finds IGameManager
using GameService.Core.GameLogic;        // Finds GameManager and IGameStateMachine
using GameService.State.Interfaces;     // Finds IHandEvaluator
using GameService.Providers;            // Finds TableStateDtoProvider

namespace GameService.Dependencies
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddGameServices(this IServiceCollection services)
        {
            // 1. Map the manager interface to your concrete GameManager class
            services.AddSingleton<IGameManager, GameService.Core.GameLogic.GameManager>();

            // 2. Map your state machines so they can be injected as an IEnumerable
            services.AddTransient<IGameStateMachine, FiveCardDrawStateMachine>();
            services.AddTransient<IGameStateMachine, TexasHoldemStateMachine>();

            // 3. Map your DTO state provider
            services.AddTransient<ITableStateDtoProvider, TableStateDtoProvider>();
            
            // 4. Map your hand evaluator if it has an interface
            services.AddTransient<IHandEvaluator, HandEvaluator>();

            return services;
        }

        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            return services;
        }
    }
}