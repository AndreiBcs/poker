namespace Source.Features.User
{
    public static class UserExtensions
    {
        public static IServiceCollection AddUserServices(this IServiceCollection services)
        {
            services.AddScoped<UserServices>();
            return services;
        }
    }
}